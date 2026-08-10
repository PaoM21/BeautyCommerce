using BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderStatus;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Orders;

public class UpdateOrderStatusCommandHandlerTests
{
    [Fact]
    public async Task Should_Call_Loyalty_When_Moving_To_Delivered()
    {
        var context = DbContextHelper.CreateDbContext();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OrderNumber = "ORD-1",
            Status = OrderStatus.Shipped,
            Total = 100m,
            OrderDate = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync(default);

        var loyaltyMock = new Mock<BeautyCommerce.Application.Common.Interfaces.ILoyaltyService>();
        loyaltyMock
            .Setup(l => l.AwardPointsForOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateOrderStatusCommandHandler(context, loyaltyMock.Object);

        var result = await handler.Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = OrderStatus.Delivered
        }, default);

        result.Should().BeTrue();

        loyaltyMock.Verify(l => l.AwardPointsForOrderAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_When_Invalid_Transition()
    {
        var context = DbContextHelper.CreateDbContext();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OrderNumber = "ORD-2",
            Status = OrderStatus.Delivered,
            Total = 50m,
            OrderDate = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync(default);

        var loyaltyMock = new Mock<BeautyCommerce.Application.Common.Interfaces.ILoyaltyService>();

        var handler = new UpdateOrderStatusCommandHandler(context, loyaltyMock.Object);

        await FluentActions.Invoking(() => handler.Handle(new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            Status = OrderStatus.Pending
        }, default)).Should().ThrowAsync<BeautyCommerce.Application.Common.Exceptions.BadRequestException>();
    }
}
