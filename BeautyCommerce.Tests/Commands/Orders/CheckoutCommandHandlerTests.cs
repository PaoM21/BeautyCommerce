using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Orders;

public class CheckoutCommandHandlerTests
{
    [Fact]
    public async Task Should_Create_Order_When_Payment_Succeeds()
    {
        var context = DbContextHelper.CreateDbContext();

        // Seed product variant and cart
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

        cart.Items.Add(new BeautyCommerce.Domain.Entities.ShoppingCartItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            Quantity = 2,
            UnitPrice = variant.Price
        });

        context.ShoppingCarts.Add(cart);
        await context.SaveChangesAsync(default);

        var currentUserMock = new Mock<BeautyCommerce.Application.Common.Interfaces.ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(userId);

        var paymentMock = new Mock<BeautyCommerce.Application.Common.Interfaces.IPaymentService>();
        paymentMock.Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "tx123" });

        var handler = new CheckoutCommandHandler(context, currentUserMock.Object, paymentMock.Object, new Microsoft.Extensions.Logging.Abstractions.NullLogger<CheckoutCommandHandler>());

        var command = new CheckoutCommand();

        var orderId = await handler.Handle(command, default);

        orderId.Should().NotBeEmpty();
        context.Orders.Count().Should().Be(1);
    }
}
