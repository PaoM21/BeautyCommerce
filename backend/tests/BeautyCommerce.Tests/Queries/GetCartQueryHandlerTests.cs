using BeautyCommerce.Application.Features.ShoppingCart.Queries.GetCart;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Queries;

public class GetCartQueryHandlerTests
{
    [Fact]
    public async Task Should_Return_Empty_Cart_If_None()
    {
        var context = DbContextHelper.CreateDbContext();

        var currentUser = new FakeCurrentUserService();

        var handler = new GetCartQueryHandler(context, currentUser);

        var dto = await handler.Handle(new GetCartQuery { UserId = Guid.NewGuid() }, default);

        dto.Should().NotBeNull();
        dto.Items.Should().BeEmpty();
    }
}
