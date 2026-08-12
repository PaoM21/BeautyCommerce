using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Services;

public class InventoryServiceTests
{
    [Fact]
    public async Task Should_Handle_Entry_Then_Exit_Then_Reject_Excessive_Exit()
    {
        // Arrange: variant starts with stock 10, matching the manual test script.
        var context = DbContextHelper.CreateDbContext();

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
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

        var afterEntry = await context.ProductVariants.FindAsync(variant.Id);
        afterEntry!.Stock.Should().Be(15);

        // Act 2: Exit of 3 -> stock should become 12.
        await service.RegisterExitAsync(
            variant.Id, 3, "Ajuste de inventario", null, default);

        var afterExit = await context.ProductVariants.FindAsync(variant.Id);
        afterExit!.Stock.Should().Be(12);

        // Act 3: Exit of 20 (more than the 12 available) must be rejected,
        // must leave stock untouched, and must not record a movement.
        var act = async () =>
            await service.RegisterExitAsync(
                variant.Id, 20, "Salida excesiva", null, default);

        await act.Should().ThrowAsync<InvalidOperationException>();

        var afterRejectedExit = await context.ProductVariants.FindAsync(variant.Id);
        afterRejectedExit!.Stock.Should().Be(12);

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
        var context = DbContextHelper.CreateDbContext();
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
        var context = DbContextHelper.CreateDbContext();

        var variant = new ProductVariant { Id = Guid.NewGuid(), Price = 1m, Stock = 5 };
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
