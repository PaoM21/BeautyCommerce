using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Orders;

public class CheckoutCommandHandlerTests
{
    [Fact]
    public async Task Should_Create_Order_When_Payment_Succeeds()
    {
        // Arrange
        var context = DbContextHelper.CreateDbContext();

        var variant = new BeautyCommerce.Domain.Entities.ProductVariant
        {
            Id = Guid.NewGuid(),
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

        var handler =
            new CheckoutCommandHandler(
                context,
                currentUserMock.Object,
                paymentMock.Object,
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
    }

}
