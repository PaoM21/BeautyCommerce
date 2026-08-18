using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BeautyCommerce.Tests.Commands.Orders;

public class CheckoutCommandHandlerTests
{
    [Fact]
    public async Task Should_Create_Order_When_Payment_Succeeds()
    {
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

        var shippingCostCalculator = new ShippingCostCalculator(
            Options.Create(new ShippingRatesOptions { BogotaCost = 8000m, NationalCost = 15000m }));

        var handler =
            new CheckoutCommandHandler(
                context,
                currentUserMock.Object,
                paymentMock.Object,
                inventoryService,
                shippingCostCalculator,
                cacheMock.Object,
                NullLogger<CheckoutCommandHandler>.Instance);

        var command = new CheckoutCommand
        {
            RecipientName = "Ana Pérez",
            Phone = "3001234567",
            AddressLine = "Calle 123 #45-67",
            City = "Bogotá",
            Department = "Cundinamarca",
        };

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
        order.SubTotal.Should().Be(20m);
        order.ShippingCost.Should().Be(8000m);
        order.Total.Should().Be(8020m);
        order.TransactionId.Should().Be("tx123");
        order.Status
            .Should()
            .Be(BeautyCommerce.Domain.Enums.OrderStatus.Paid);

        order.ShippingRecipientName.Should().Be("Ana Pérez");
        order.ShippingPhone.Should().Be("3001234567");
        order.ShippingAddressLine.Should().Be("Calle 123 #45-67");
        order.ShippingCity.Should().Be("Bogotá");
        order.ShippingDepartment.Should().Be("Cundinamarca");

        context.OrderItems
            .Count()
            .Should()
            .Be(1);

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
