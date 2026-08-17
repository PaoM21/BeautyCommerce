using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderStatus;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BeautyCommerce.Tests.Commands.Orders;

// Regression coverage for the Punto 5 / Escenario E finding: cancelling a
// Paid order silently kept its stock decremented forever, because
// UpdateOrderStatusCommandHandler never called into IInventoryService.
// These tests use SqliteDbContextHelper (not the InMemory provider) because
// the fix routes through InventoryService.RegisterEntryAsync, which relies
// on ExecuteUpdateAsync — unsupported by EF Core's InMemory provider.
public class UpdateOrderStatusCancellationRestockTests
{
    private static async Task<(Guid ProductId, Guid VariantId)> SeedProductAsync(
        ApplicationDbContext context,
        int initialStock)
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

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 25m,
            Stock = initialStock
        };

        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(default);

        return (product.Id, variant.Id);
    }

    private static async Task<Guid> SeedPaidOrderAsync(
        ApplicationDbContext context,
        params (Guid VariantId, int Quantity, decimal UnitPrice)[] items)
    {
        var order = new Order
        {
            UserId = Guid.NewGuid(),
            OrderNumber = $"ORD-{Guid.NewGuid():N}",
            Status = OrderStatus.Paid,
            OrderDate = DateTime.UtcNow,
            SubTotal = items.Sum(i => i.Quantity * i.UnitPrice),
            Total = items.Sum(i => i.Quantity * i.UnitPrice)
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync(default);

        foreach (var item in items)
        {
            context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductVariantId = item.VariantId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Total = item.Quantity * item.UnitPrice
            });
        }

        await context.SaveChangesAsync(default);

        return order.Id;
    }

    private static UpdateOrderStatusCommandHandler BuildHandler(
        ApplicationDbContext context,
        out Mock<ILoyaltyService> loyaltyMock)
    {
        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.InvalidateTagAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var inventoryService = new InventoryService(context, cacheMock.Object);

        loyaltyMock = new Mock<ILoyaltyService>();
        loyaltyMock
            .Setup(l => l.AwardPointsForOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        return new UpdateOrderStatusCommandHandler(
            context, loyaltyMock.Object, cacheMock.Object, inventoryService, currentUserMock.Object);
    }

    [Fact]
    public async Task Cancelling_Paid_Order_With_One_Line_Restores_Exact_Stock()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var sqliteConnection = connection;

        var (_, variantId) = await SeedProductAsync(context, initialStock: 47);
        var orderId = await SeedPaidOrderAsync(context, (variantId, 5, 25m));

        // Simulate the stock decrement that already happened at checkout time.
        var cacheMock = new Mock<ICacheService>();
        var setupInventory = new InventoryService(context, cacheMock.Object);
        await setupInventory.TryRegisterExitAsync(variantId, 5, "Salida por pedido de prueba", null, default);

        (await context.ProductVariants.AsNoTracking().Where(v => v.Id == variantId).Select(v => v.Stock).FirstAsync())
            .Should().Be(42, "sanity check: stock should be decremented before cancellation");

        var handler = BuildHandler(context, out _);

        var result = await handler.Handle(
            new UpdateOrderStatusCommand { OrderId = orderId, Status = OrderStatus.Cancelled },
            default);

        result.Should().BeTrue();

        var restoredStock = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => v.Stock)
            .FirstAsync();

        restoredStock.Should().Be(47, "cancelling a Paid order must restore exactly the quantity that was reserved at checkout");

        var order = await context.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancelling_Paid_Order_With_Multiple_Lines_Restores_All_Quantities()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var sqliteConnection = connection;

        var (_, variant1) = await SeedProductAsync(context, initialStock: 30);
        var (_, variant2) = await SeedProductAsync(context, initialStock: 10);

        var orderId = await SeedPaidOrderAsync(
            context,
            (variant1, 4, 25m),
            (variant2, 2, 15m));

        var cacheMock = new Mock<ICacheService>();
        var setupInventory = new InventoryService(context, cacheMock.Object);
        await setupInventory.TryRegisterExitAsync(variant1, 4, "Salida por pedido de prueba", null, default);
        await setupInventory.TryRegisterExitAsync(variant2, 2, "Salida por pedido de prueba", null, default);

        var handler = BuildHandler(context, out _);

        await handler.Handle(
            new UpdateOrderStatusCommand { OrderId = orderId, Status = OrderStatus.Cancelled },
            default);

        var stock1 = await context.ProductVariants.AsNoTracking().Where(v => v.Id == variant1).Select(v => v.Stock).FirstAsync();
        var stock2 = await context.ProductVariants.AsNoTracking().Where(v => v.Id == variant2).Select(v => v.Stock).FirstAsync();

        stock1.Should().Be(30);
        stock2.Should().Be(10);
    }

    [Fact]
    public async Task Cancelling_Paid_Order_Creates_Entry_InventoryMovement()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var sqliteConnection = connection;

        var (_, variantId) = await SeedProductAsync(context, initialStock: 47);
        var orderId = await SeedPaidOrderAsync(context, (variantId, 5, 25m));

        var cacheMock = new Mock<ICacheService>();
        var setupInventory = new InventoryService(context, cacheMock.Object);
        await setupInventory.TryRegisterExitAsync(variantId, 5, "Salida por pedido de prueba", null, default);

        var order = await context.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        var handler = BuildHandler(context, out _);

        await handler.Handle(
            new UpdateOrderStatusCommand { OrderId = orderId, Status = OrderStatus.Cancelled },
            default);

        var entryMovement = await context.InventoryMovements
            .AsNoTracking()
            .Where(m => m.ProductVariantId == variantId && m.IsEntry)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        entryMovement.Should().NotBeNull("cancelling must leave an auditable entry movement, not a silent stock change");
        entryMovement!.Quantity.Should().Be(5);
        entryMovement.Reason.Should().Contain(order.OrderNumber, "the restitution reason should reference the cancelled order for traceability");
    }

    [Fact]
    public async Task Processing_To_Cancelled_Is_Still_Rejected_And_Does_Not_Restock()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var sqliteConnection = connection;

        var (_, variantId) = await SeedProductAsync(context, initialStock: 47);
        var orderId = await SeedPaidOrderAsync(context, (variantId, 5, 25m));

        var cacheMock = new Mock<ICacheService>();
        var setupInventory = new InventoryService(context, cacheMock.Object);
        await setupInventory.TryRegisterExitAsync(variantId, 5, "Salida por pedido de prueba", null, default);

        var order = await context.Orders.FirstAsync(o => o.Id == orderId);
        order.Status = OrderStatus.Processing;
        await context.SaveChangesAsync(default);

        var handler = BuildHandler(context, out _);

        await FluentActions
            .Invoking(() => handler.Handle(
                new UpdateOrderStatusCommand { OrderId = orderId, Status = OrderStatus.Cancelled },
                default))
            .Should().ThrowAsync<BadRequestException>();

        var stock = await context.ProductVariants.AsNoTracking().Where(v => v.Id == variantId).Select(v => v.Stock).FirstAsync();
        stock.Should().Be(42, "a rejected transition must never trigger stock restoration");
    }

    [Fact]
    public async Task Already_Cancelled_Order_Rejects_Second_Cancellation_And_Does_Not_Double_Restock()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var sqliteConnection = connection;

        var (_, variantId) = await SeedProductAsync(context, initialStock: 47);
        var orderId = await SeedPaidOrderAsync(context, (variantId, 5, 25m));

        var cacheMock = new Mock<ICacheService>();
        var setupInventory = new InventoryService(context, cacheMock.Object);
        await setupInventory.TryRegisterExitAsync(variantId, 5, "Salida por pedido de prueba", null, default);

        var firstHandler = BuildHandler(context, out _);
        await firstHandler.Handle(
            new UpdateOrderStatusCommand { OrderId = orderId, Status = OrderStatus.Cancelled },
            default);

        (await context.ProductVariants.AsNoTracking().Where(v => v.Id == variantId).Select(v => v.Stock).FirstAsync())
            .Should().Be(47, "sanity check: first cancellation restored stock");

        var secondHandler = BuildHandler(context, out _);

        await FluentActions
            .Invoking(() => secondHandler.Handle(
                new UpdateOrderStatusCommand { OrderId = orderId, Status = OrderStatus.Cancelled },
                default))
            .Should().ThrowAsync<BadRequestException>("Cancelled is terminal — no transition is valid from it");

        var stock = await context.ProductVariants.AsNoTracking().Where(v => v.Id == variantId).Select(v => v.Stock).FirstAsync();
        stock.Should().Be(47, "a second cancellation attempt must not restock a second time");
    }

    [Fact]
    public async Task Cancelling_Paid_Order_Does_Not_Alter_Historical_Price_Or_Award_Loyalty_Points()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var sqliteConnection = connection;

        var (_, variantId) = await SeedProductAsync(context, initialStock: 47);
        var orderId = await SeedPaidOrderAsync(context, (variantId, 5, 25m));

        var cacheMock = new Mock<ICacheService>();
        var setupInventory = new InventoryService(context, cacheMock.Object);
        await setupInventory.TryRegisterExitAsync(variantId, 5, "Salida por pedido de prueba", null, default);

        var handler = BuildHandler(context, out var loyaltyMock);

        await handler.Handle(
            new UpdateOrderStatusCommand { OrderId = orderId, Status = OrderStatus.Cancelled },
            default);

        var item = await context.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == orderId);
        item.UnitPrice.Should().Be(25m, "cancellation must never rewrite the historical unit price charged to the customer");

        loyaltyMock.Verify(
            l => l.AwardPointsForOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "cancellation is not a delivery — no loyalty points should be awarded");
    }
}
