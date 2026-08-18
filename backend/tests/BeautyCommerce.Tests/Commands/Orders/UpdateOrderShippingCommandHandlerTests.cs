using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderShipping;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Orders;

public class UpdateOrderShippingCommandHandlerTests
{
    [Fact]
    public async Task Should_Set_Carrier_TrackingNumber_And_ShippedAt()
    {
        var context = DbContextHelper.CreateDbContext();

        var order = new Order
        {
            OrderNumber = "ORD-TEST-1",
            Status = OrderStatus.Processing,
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var cacheMock = new Mock<ICacheService>();
        var handler = new UpdateOrderShippingCommandHandler(context, cacheMock.Object);

        var command = new UpdateOrderShippingCommand
        {
            OrderId = order.Id,
            Carrier = "Envía",
            TrackingNumber = "ENV123456",
        };

        var result = await handler.Handle(command, default);

        result.Should().BeTrue();

        var updated = context.Orders.First(x => x.Id == order.Id);
        updated.Carrier.Should().Be("Envía");
        updated.TrackingNumber.Should().Be("ENV123456");
        updated.ShippedAt.Should().NotBeNull();

        cacheMock.Verify(x => x.InvalidateTagAsync("Orders"), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Overwrite_ShippedAt_When_Already_Set()
    {
        var context = DbContextHelper.CreateDbContext();

        var originalShippedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var order = new Order
        {
            OrderNumber = "ORD-TEST-2",
            Status = OrderStatus.Shipped,
            Carrier = "Interrapidísimo",
            TrackingNumber = "OLD123",
            ShippedAt = originalShippedAt,
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var cacheMock = new Mock<ICacheService>();
        var handler = new UpdateOrderShippingCommandHandler(context, cacheMock.Object);

        var command = new UpdateOrderShippingCommand
        {
            OrderId = order.Id,
            Carrier = "Interrapidísimo",
            TrackingNumber = "NEW456",
        };

        await handler.Handle(command, default);

        var updated = context.Orders.First(x => x.Id == order.Id);
        updated.TrackingNumber.Should().Be("NEW456");
        updated.ShippedAt.Should().Be(originalShippedAt);
    }

    [Fact]
    public async Task Should_Return_False_When_Order_Does_Not_Exist()
    {
        var context = DbContextHelper.CreateDbContext();
        var cacheMock = new Mock<ICacheService>();
        var handler = new UpdateOrderShippingCommandHandler(context, cacheMock.Object);

        var result = await handler.Handle(
            new UpdateOrderShippingCommand
            {
                OrderId = Guid.NewGuid(),
                Carrier = "Envía",
                TrackingNumber = "X",
            },
            default);

        result.Should().BeFalse();
    }
}
