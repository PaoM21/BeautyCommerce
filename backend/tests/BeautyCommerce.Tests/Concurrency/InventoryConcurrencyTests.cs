using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BeautyCommerce.Tests.Concurrency;

// These tests run against a REAL PostgreSQL database, not the InMemory
// provider the rest of the suite uses. EF Core's InMemory provider doesn't
// model row-level locking or transaction isolation at all, so it would hide
// exactly the race condition this test exists to prove or disprove.
//
// Requires a local BeautyCommerceDb reachable at the connection string
// below. Tagged "Integration" so it can be excluded from the fast default
// run (`dotnet test --filter Category!=Integration`) and from CI, which has
// no Postgres service configured today.
[Trait("Category", "Integration")]
public class InventoryConcurrencyTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private Guid _productId;
    private Guid _variantId;

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    public async Task InitializeAsync()
    {
        await using var context = NewContext();

        var brandId = await context.Brands.Select(x => x.Id).FirstAsync();
        var categoryId = await context.Categories.Select(x => x.Id).FirstAsync();

        var product = new Product
        {
            Name = "QA Concurrency Test Product",
            Slug = $"qa-concurrency-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by InventoryConcurrencyTests; removed in DisposeAsync.",
            BrandId = brandId,
            CategoryId = categoryId
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-CONC-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 10,
            MinimumStock = 0
        };

        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        _productId = product.Id;
        _variantId = variant.Id;
    }

    public async Task DisposeAsync()
    {
        await using var context = NewContext();

        context.InventoryMovements.RemoveRange(
            context.InventoryMovements.Where(x => x.ProductVariantId == _variantId));

        await context.SaveChangesAsync();

        var variant = await context.ProductVariants.FindAsync(_variantId);
        if (variant != null)
            context.ProductVariants.Remove(variant);

        var product = await context.Products.FindAsync(_productId);
        if (product != null)
            context.Products.Remove(product);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Deterministic_Interleaved_Exits_Cause_A_Lost_Update()
    {
        // Manually reproduces, step by step, exactly what
        // InventoryService.RegisterExitAsync does today — read variant,
        // check stock, decrement in memory, add a movement, SaveChanges —
        // but with two "requests" deliberately interleaved so both read the
        // original stock before either writes, mirroring two admins racing
        // on the same SKU. This is deterministic (not dependent on OS/
        // network scheduling), so it proves the mechanism rather than
        // merely observing that it sometimes happens.
        await using var contextA = NewContext();
        await using var contextB = NewContext();

        var variantA = await contextA.ProductVariants.FirstAsync(x => x.Id == _variantId);
        var variantB = await contextB.ProductVariants.FirstAsync(x => x.Id == _variantId);

        variantA.Stock.Should().Be(10);
        variantB.Stock.Should().Be(10, "B reads before A has written anything, same as a real concurrent request");

        // Both pass InventoryService's "is there enough stock?" guard using
        // their own stale read of 10 — neither has any way to know about
        // the other yet.
        (variantA.Stock >= 7).Should().BeTrue();
        (variantB.Stock >= 7).Should().BeTrue();

        variantA.Stock -= 7;
        contextA.InventoryMovements.Add(new InventoryMovement
        {
            ProductVariantId = _variantId,
            Quantity = 7,
            IsEntry = false,
            Reason = "QA concurrency test - request A"
        });
        await contextA.SaveChangesAsync(); // commits Stock = 3

        variantB.Stock -= 7; // computed from B's own stale read of 10, unaware A already committed
        contextB.InventoryMovements.Add(new InventoryMovement
        {
            ProductVariantId = _variantId,
            Quantity = 7,
            IsEntry = false,
            Reason = "QA concurrency test - request B"
        });
        await contextB.SaveChangesAsync(); // overwrites Stock = 3 again, silently discarding A's write

        await using var verify = NewContext();

        var finalVariant = await verify.ProductVariants
            .AsNoTracking()
            .FirstAsync(x => x.Id == _variantId);

        var movements = await verify.InventoryMovements
            .AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId)
            .ToListAsync();

        finalVariant.Stock.Should().Be(
            3,
            "both writes succeeded but B's UPDATE silently overwrote A's — a lost update, " +
            "not the -4 you'd get from two truly independent decrements");

        movements.Should().HaveCount(2, "both exits were recorded as having happened");

        movements.Sum(x => x.Quantity).Should().Be(
            14,
            "the movement history says 14 units left the building, 4 more than the 10 that ever existed");
    }

    [Fact]
    public async Task Concurrent_RegisterExitAsync_Calls_Never_Both_Succeed()
    {
        // 6.2.3: same scenario as before (two real concurrent
        // RegisterExitAsync(7) calls against a Stock = 10 variant, each
        // driven through its own DbContext and explicit transaction, the
        // same way TransactionBehavior wraps every Command in the real
        // pipeline), but now asserting the CORRECT outcome that
        // InventoryService's atomic UPDATE ... WHERE Stock >= quantity
        // (6.2.2) guarantees: exactly one request wins, the other is
        // rejected with the normal "insufficient stock" business
        // exception — never both succeeding, and never a silent lost
        // update.
        //
        // A single run isn't enough evidence for a race condition fix —
        // the whole point is that it must hold up under real scheduling
        // every time, not just when we get lucky. This repeats the race
        // several times, resetting stock between rounds, and every single
        // round must land on the same correct outcome.
        const int rounds = 10;

        async Task<bool> RunExit(ApplicationDbContext context)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var cache = new Mock<ICacheService>();
            var service = new InventoryService(context, cache.Object);

            try
            {
                await service.RegisterExitAsync(
                    _variantId, 7, "QA concurrency race", null, default);

                await transaction.CommitAsync();
                return true;
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        for (var round = 1; round <= rounds; round++)
        {
            await using (var reset = NewContext())
            {
                await reset.InventoryMovements
                    .Where(x => x.ProductVariantId == _variantId)
                    .ExecuteDeleteAsync();

                await reset.ProductVariants
                    .Where(x => x.Id == _variantId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Stock, 10));
            }

            await using var contextA = NewContext();
            await using var contextB = NewContext();

            var taskA = RunExit(contextA);
            var taskB = RunExit(contextB);

            var results = await Task.WhenAll(taskA, taskB);

            await using var verify = NewContext();

            var finalVariant = await verify.ProductVariants
                .AsNoTracking()
                .FirstAsync(x => x.Id == _variantId);

            var movements = await verify.InventoryMovements
                .AsNoTracking()
                .Where(x => x.ProductVariantId == _variantId)
                .ToListAsync();

            results.Count(x => x).Should().Be(1, $"round {round}: exactly one exit should be accepted");
            finalVariant.Stock.Should().Be(3, $"round {round}: stock must reflect exactly one accepted exit of 7");
            movements.Should().HaveCount(1, $"round {round}: only the accepted exit should leave a movement");
            movements.Sum(x => x.Quantity).Should().Be(7, $"round {round}: movement history must match the single accepted exit");
        }
    }
}
