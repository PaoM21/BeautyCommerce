using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Concurrency;

[Trait("Category", "Integration")]
public class AddCartItemConcurrencyTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private Guid _productId;
    private Guid _variantId;
    private readonly List<Guid> _usedUserIds = [];

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options,
            null);

    public async Task InitializeAsync()
    {
        await using var context = NewContext();

        var brandId = await context.Brands.Select(x => x.Id).FirstAsync();
        var categoryId = await context.Categories.Select(x => x.Id).FirstAsync();

        var product = new Product
        {
            Name = "QA Cart Race Product",
            Slug = $"qa-cart-race-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by AddCartItemConcurrencyTests; removed in DisposeAsync.",
            BrandId = brandId,
            CategoryId = categoryId
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-CART-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            // Ample stock: this test isolates the ShoppingCart-duplication
            // fix from the already-proven oversell protection (6.2).
            Stock = 1000,
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

        // Scoped precisely to the user ids this test itself generated and
        // used (tracked in _usedUserIds as the test runs) — never a blanket
        // "delete anything that looks empty", which could touch real,
        // unrelated data.
        if (_usedUserIds.Count > 0)
        {
            var cartIds = await context.ShoppingCarts
                .Where(c => _usedUserIds.Contains(c.UserId))
                .Select(c => c.Id)
                .ToListAsync();

            await context.ShoppingCartItems.Where(i => cartIds.Contains(i.ShoppingCartId)).ExecuteDeleteAsync();
            await context.ShoppingCarts.Where(c => cartIds.Contains(c.Id)).ExecuteDeleteAsync();
        }

        var variant = await context.ProductVariants.FindAsync(_variantId);
        if (variant != null) context.ProductVariants.Remove(variant);

        var product = await context.Products.FindAsync(_productId);
        if (product != null) context.Products.Remove(product);

        await context.SaveChangesAsync();
    }

    private async Task<Guid> RunAddToCartAsync(ApplicationDbContext context, Guid userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var handler = new AddCartItemCommandHandler(context, currentUser.Object);

        var behavior = new TransactionBehavior<AddCartItemCommand, Guid>(
            context, NullLogger<TransactionBehavior<AddCartItemCommand, Guid>>.Instance);

        var command = new AddCartItemCommand
        {
            Item = new AddCartItemDto { ProductVariantId = _variantId, Quantity = 1 }
        };

        return await behavior.Handle(command, _ => handler.Handle(command, default), default);
    }

    [Fact]
    public async Task Two_Concurrent_AddCartItem_Requests_For_A_Brand_New_User_Resolve_To_The_Same_Cart()
    {
        const int rounds = 10;

        for (var round = 1; round <= rounds; round++)
        {
            var userId = Guid.NewGuid(); // brand-new user every round: zero ShoppingCarts exist yet
            _usedUserIds.Add(userId);

            await using var contextA = NewContext();
            await using var contextB = NewContext();

            var taskA = RunAddToCartAsync(contextA, userId);
            var taskB = RunAddToCartAsync(contextB, userId);

            // Neither task is awaited individually first — both real,
            // independent Npgsql connections race genuinely via
            // Task.WhenAll, exactly the scenario 8.3.1/8.3.2 identified.
            var results = await Task.WhenAll(taskA, taskB);

            results[0].Should().NotBeEmpty($"round {round}: request A must succeed, not throw");
            results[1].Should().NotBeEmpty($"round {round}: request B must succeed, not throw — no unhandled 500 from an aborted transaction");
            results[0].Should().Be(results[1], $"round {round}: both concurrent requests must resolve to the exact same ShoppingCart");

            await using var verify = NewContext();

            var cartsForUser = await verify.ShoppingCarts.AsNoTracking()
                .Where(c => c.UserId == userId)
                .ToListAsync();

            cartsForUser.Should().ContainSingle(
                $"round {round}: PostgreSQL must end with exactly one ShoppingCart for this user — the UNIQUE index must have rejected the loser's insert");

            var items = await verify.ShoppingCartItems.AsNoTracking()
                .Where(i => i.ShoppingCartId == cartsForUser[0].Id)
                .ToListAsync();

            items.Should().ContainSingle(
                $"round {round}: both requests added the same variant, so they must have merged into one cart item, not two");
            items[0].Quantity.Should().Be(2,
                $"round {round}: the losing request must have successfully continued adding its own item to the recovered cart");
        }
    }
}
