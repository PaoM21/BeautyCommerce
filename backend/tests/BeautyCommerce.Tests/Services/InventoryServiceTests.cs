using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BeautyCommerce.Tests.Services;

public class InventoryServiceTests
{
    // Unlike the InMemory provider, SQLite enforces real foreign keys, so a
    // ProductVariant needs an actual Product (and that Product a Brand and
    // Category) to insert successfully.
    private static async Task<Guid> SeedProductAsync(ApplicationDbContext context)
    {
        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };

        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync(default);

        var product = new Product
        {
            Name = "QA Product",
            Slug = $"qa-{Guid.NewGuid():N}",
            BrandId = brand.Id,
            CategoryId = category.Id
        };

        context.Products.Add(product);
        await context.SaveChangesAsync(default);

        return product.Id;
    }

    [Fact]
    public async Task Should_Handle_Entry_Then_Exit_Then_Reject_Excessive_Exit()
    {
        // Arrange: variant starts with stock 10, matching the manual test script.
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var productId = await SeedProductAsync(context);

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 10
        };

        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(default);

        var cache = new Mock<ICacheService>();

        var service = new InventoryService(context, cache.Object);

        // Act 1: Entry of 5 -> stock should become 15.
        await service.RegisterEntryAsync(
            variant.Id, 5, "Compra de mercancía", null, default);

        // RegisterEntryAsync/RegisterExitAsync now write via ExecuteUpdateAsync,
        // which goes straight to the database and does not update the
        // context's change tracker. The already-tracked `variant` instance
        // (and anything the identity map would resolve to it, including
        // FindAsync) still shows its pre-update in-memory value, so
        // verification reads here use AsNoTracking to see the real
        // persisted state.
        var afterEntry = await context.ProductVariants
            .AsNoTracking()
            .FirstAsync(x => x.Id == variant.Id);
        afterEntry.Stock.Should().Be(15);

        // Act 2: Exit of 3 -> stock should become 12.
        await service.RegisterExitAsync(
            variant.Id, 3, "Ajuste de inventario", null, default);

        var afterExit = await context.ProductVariants
            .AsNoTracking()
            .FirstAsync(x => x.Id == variant.Id);
        afterExit.Stock.Should().Be(12);

        // Act 3: Exit of 20 (more than the 12 available) must be rejected,
        // must leave stock untouched, and must not record a movement.
        var act = async () =>
            await service.RegisterExitAsync(
                variant.Id, 20, "Salida excesiva", null, default);

        await act.Should().ThrowAsync<InvalidOperationException>();

        var afterRejectedExit = await context.ProductVariants
            .AsNoTracking()
            .FirstAsync(x => x.Id == variant.Id);
        afterRejectedExit.Stock.Should().Be(12);

        var movements = context.InventoryMovements
            .Where(x => x.ProductVariantId == variant.Id)
            .ToList();

        movements.Should().HaveCount(2);
        movements.Should().Contain(x => x.IsEntry && x.Quantity == 5);
        movements.Should().Contain(x => !x.IsEntry && x.Quantity == 3);

        // The two successful operations must each invalidate the Products and Inventory cache tags.
        cache.Verify(
            x => x.InvalidateTagAsync("Products"),
            Times.Exactly(2));
        cache.Verify(
            x => x.InvalidateTagAsync("Inventory"),
            Times.Exactly(2));
    }

    [Fact]
    public async Task RegisterEntryAsync_Should_Throw_When_Variant_Does_Not_Exist()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var cache = new Mock<ICacheService>();
        var service = new InventoryService(context, cache.Object);

        var act = async () =>
            await service.RegisterEntryAsync(
                Guid.NewGuid(), 5, "Motivo", null, default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RegisterExitAsync_Should_Throw_When_Quantity_Is_Zero_Or_Negative()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var productId = await SeedProductAsync(context);

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 1m,
            Stock = 5
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(default);

        var cache = new Mock<ICacheService>();
        var service = new InventoryService(context, cache.Object);

        var act = async () =>
            await service.RegisterExitAsync(variant.Id, 0, "Motivo", null, default);

        await act.Should().ThrowAsync<ArgumentException>();

        var unchanged = await context.ProductVariants.FindAsync(variant.Id);
        unchanged!.Stock.Should().Be(5);
    }
}
