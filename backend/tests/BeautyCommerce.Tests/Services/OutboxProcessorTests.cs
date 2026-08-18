using System.Text.Json;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Users.DTOs;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Services;

public class OutboxProcessorTests
{
    private static Product CreateProductWithVariant()
    {
        var product = new Product
        {
            Name = "Labial Mate Rojo",
            Slug = $"labial-{Guid.NewGuid():N}",
        };

        product.Variants.Add(new ProductVariant
        {
            SKU = $"SKU-{Guid.NewGuid():N}"[..15],
            Price = 45000m,
        });

        return product;
    }

    [Fact]
    public async Task Should_Send_Order_Confirmation_Email_For_OrderCreated_Message()
    {
        var context = DbContextHelper.CreateDbContext();
        var userId = Guid.NewGuid();

        var product = CreateProductWithVariant();
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var order = new Order
        {
            UserId = userId,
            OrderNumber = "ORD-TEST-1",
            SubTotal = 45000m,
            ShippingCost = 8000m,
            Total = 53000m,
            ShippingRecipientName = "Ana Pérez",
            ShippingAddressLine = "Calle 123",
            ShippingCity = "Bogotá",
            ShippingDepartment = "Cundinamarca",
        };
        order.Items.Add(new OrderItem
        {
            ProductVariantId = product.Variants.First().Id,
            Quantity = 1,
            UnitPrice = 45000m,
        });
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "OrderCreated",
            OccurredOn = DateTime.UtcNow,
            Content = JsonSerializer.Serialize(new
            {
                order.Id,
                order.OrderNumber,
                order.Total,
                order.UserId,
            }),
        };
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var emailMock = new Mock<IEmailService>();

        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(x => x.GetUsersByIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(userId)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>
            {
                [userId] = new UserDto { Id = userId, Email = "cliente@test.com", FullName = "Ana Pérez" },
            });

        await OutboxProcessor.ProcessMessageAsync(
            context, emailMock.Object, userServiceMock.Object, message,
            NullLogger.Instance, default);

        emailMock.Verify(
            x => x.SendAsync(
                "cliente@test.com",
                It.Is<string>(subject => subject.Contains("ORD-TEST-1")),
                It.Is<string>(html => html.Contains("Labial Mate Rojo")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        message.ProcessedOn.Should().NotBeNull();
        message.Error.Should().BeNull();
    }

    [Fact]
    public async Task Should_Record_Error_And_Leave_Unprocessed_When_Order_Not_Found()
    {
        var context = DbContextHelper.CreateDbContext();

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "OrderCreated",
            OccurredOn = DateTime.UtcNow,
            Content = JsonSerializer.Serialize(new
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-GHOST",
                Total = 1000m,
                UserId = Guid.NewGuid(),
            }),
        };
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var emailMock = new Mock<IEmailService>();
        var userServiceMock = new Mock<IUserService>();

        await OutboxProcessor.ProcessMessageAsync(
            context, emailMock.Object, userServiceMock.Object, message,
            NullLogger.Instance, default);

        message.ProcessedOn.Should().BeNull();
        message.Error.Should().NotBeNullOrEmpty();

        emailMock.Verify(
            x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Mark_Unknown_Message_Types_As_Processed_Without_Sending_Email()
    {
        var context = DbContextHelper.CreateDbContext();

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "SomeOtherEvent",
            OccurredOn = DateTime.UtcNow,
            Content = "{}",
        };
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var emailMock = new Mock<IEmailService>();
        var userServiceMock = new Mock<IUserService>();

        await OutboxProcessor.ProcessMessageAsync(
            context, emailMock.Object, userServiceMock.Object, message,
            NullLogger.Instance, default);

        message.ProcessedOn.Should().NotBeNull();
        message.Error.Should().BeNull();

        emailMock.Verify(
            x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
