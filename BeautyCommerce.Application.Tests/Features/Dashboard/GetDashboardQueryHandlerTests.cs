using BeautyCommerce.Application.Features.Dashboard.Queries.GetDashboard;
using BeautyCommerce.Application.Tests.Helpers;
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

        var userService = new Mock<BeautyCommerce.Application.Common.Interfaces.IUserService>();
        userService.Setup(x => x.GetTotalCustomersAsync()).ReturnsAsync(0);

        var handler = new GetDashboardQueryHandler(context, userService.Object);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        result.TotalProducts.Should().Be(2);
    }
}
