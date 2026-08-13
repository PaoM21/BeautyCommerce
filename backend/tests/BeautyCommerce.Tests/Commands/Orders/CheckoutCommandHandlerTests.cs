using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Orders;

public class CheckoutCommandHandlerTests
{
    [Fact]
    public async Task Should_Create_Order_When_Payment_Succeeds()
    {
        // Arrange. Uses SQLite (not the InMemory provider) because checkout
        // now reserves stock via InventoryService.TryRegisterExitAsync,
        // which relies on ExecuteUpdateAsync — see InventoryServiceTests
        // for why. SQLite also enforces real foreign keys, hence the
        // Brand/Category/Product seeding below.
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Product",
            Slug = $"qa-{Guid.NewGuid():N}",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 5
        };

        context.ProductVariants.Add(variant);

        var userId = Guid.NewGuid();

        var cart = new BeautyCommerce.Domain.Entities.ShoppingCart
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        cart.Items.Add(
            new BeautyCommerce.Domain.Entities.ShoppingCartItem
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variant.Id,
                Quantity = 2,
                UnitPrice = variant.Price
            });

        context.ShoppingCarts.Add(cart);

        await context.SaveChangesAsync();

        var currentUserMock =
            new Mock<ICurrentUserService>();

        currentUserMock
            .Setup(x => x.UserId)
            .Returns(userId);

        var paymentMock =
            new Mock<IPaymentService>();

        paymentMock
            .Setup(p =>
                p.CreatePaymentAsync(
                    It.IsAny<decimal>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .ReturnsAsync(
                PaymentResult.Succeeded("tx123"));

        var cacheMock = new Mock<ICacheService>();
        var inventoryService = new InventoryService(context, cacheMock.Object);

        var handler =
            new CheckoutCommandHandler(
                context,
                currentUserMock.Object,
                paymentMock.Object,
                inventoryService,
                NullLogger<CheckoutCommandHandler>.Instance);

        var command = new CheckoutCommand();

        // Act
        var orderId =
            await handler.Handle(
                command,
                CancellationToken.None);

        // Assert
        orderId.Should().NotBeEmpty();

        var order =
            context.Orders
                .FirstOrDefault();

        order.Should().NotBeNull();

        order!.UserId.Should().Be(userId);
        order.Total.Should().Be(20m);
        order.SubTotal.Should().Be(20m);
        order.TransactionId.Should().Be("tx123");
        order.Status
            .Should()
            .Be(BeautyCommerce.Domain.Enums.OrderStatus.Paid);

        context.OrderItems
            .Count()
            .Should()
            .Be(1);

        // Stock and the InventoryMovement now come from
        // IInventoryService.TryRegisterExitAsync, not from checkout
        // mutating ProductVariant.Stock directly — confirm both actually
        // happened. ExecuteUpdateAsync bypasses the change tracker, so
        // read fresh via AsNoTracking.
        var updatedVariant = await context.ProductVariants
            .AsNoTracking()
            .FirstAsync(x => x.Id == variant.Id);
        updatedVariant.Stock.Should().Be(3);

        var movements = await context.InventoryMovements
            .AsNoTracking()
            .Where(x => x.ProductVariantId == variant.Id)
            .ToListAsync();
        movements.Should().HaveCount(1);
        movements[0].Quantity.Should().Be(2);
        movements[0].IsEntry.Should().BeFalse();
    }
}
