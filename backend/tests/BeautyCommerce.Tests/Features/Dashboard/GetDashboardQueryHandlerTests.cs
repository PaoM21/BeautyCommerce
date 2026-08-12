using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Dashboard.Queries.GetDashboard;
using BeautyCommerce.Application.Features.Users.DTOs;
using BeautyCommerce.Application.Tests.Helpers;
using BeautyCommerce.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BeautyCommerce.Application.Tests.Features.Dashboard;

public class GetDashboardQueryHandlerTests
{
    [Fact]
    public async Task Should_Return_Total_Products()
    {
        var context = ApplicationDbContextFactory.Create();

        context.Products.Add(new BeautyCommerce.Domain.Entities.Product());
        context.Products.Add(new BeautyCommerce.Domain.Entities.Product());

        await context.SaveChangesAsync();

        var userService = new Mock<IUserService>();
        userService.Setup(x => x.GetTotalCustomersAsync()).ReturnsAsync(0);
        userService
            .Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var handler = new GetDashboardQueryHandler(context, userService.Object);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        result.TotalProducts.Should().Be(2);
    }

    [Fact]
    public async Task Should_Resolve_Customer_Name_For_Last_Orders()
    {
        var context = ApplicationDbContextFactory.Create();

        var knownCustomerId = Guid.NewGuid();
        var unknownCustomerId = Guid.NewGuid();

        context.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            UserId = knownCustomerId,
            OrderNumber = "ORD-1",
            Status = BeautyCommerce.Domain.Enums.OrderStatus.Paid,
            Total = 100m,
            OrderDate = DateTime.UtcNow
        });

        context.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            UserId = unknownCustomerId,
            OrderNumber = "ORD-2",
            Status = BeautyCommerce.Domain.Enums.OrderStatus.Paid,
            Total = 50m,
            OrderDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var userService = new Mock<IUserService>();
        userService.Setup(x => x.GetTotalCustomersAsync()).ReturnsAsync(0);
        userService
            .Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>
            {
                [knownCustomerId] = new UserDto
                {
                    Id = knownCustomerId,
                    FullName = "Ana Torres",
                    Email = "ana@test.local"
                }
            });

        var handler = new GetDashboardQueryHandler(context, userService.Object);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // A resolved user shows their name instead of the raw UserId.
        result.LastOrders.Single(x => x.OrderNumber == "ORD-1").Customer
            .Should().Be("Ana Torres");

        // A user that can't be resolved falls back to the UserId rather than
        // silently showing nothing.
        result.LastOrders.Single(x => x.OrderNumber == "ORD-2").Customer
            .Should().Be(unknownCustomerId.ToString());
    }
}
