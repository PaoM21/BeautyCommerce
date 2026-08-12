using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Dashboard.DTOs;
using BeautyCommerce.Application.Features.Dashboard.Queries.GetDashboard;
using BeautyCommerce.Application.Features.Loyalty.DTOs;
using BeautyCommerce.Application.Features.Loyalty.Queries.GetMyLoyalty;
using BeautyCommerce.Application.Features.Orders.DTOs;
using BeautyCommerce.Application.Features.Orders.Queries.GetMyOrders;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Application.Features.ShoppingCart.Queries.GetCart;
using BeautyCommerce.Application.Features.Wishlist.DTOs;
using BeautyCommerce.Application.Features.Wishlist.Queries.GetWishlist;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeautyCommerce.Tests.Behaviors;

public class CachingBehaviorTests
{
    private static Mock<ICurrentUserService> CurrentUserMock(Guid? userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId);
        return mock;
    }

    [Fact]
    public async Task Should_Not_Cache_ShoppingCart_Queries()
    {
        var cache = new Mock<ICacheService>();
        var currentUser = CurrentUserMock(Guid.NewGuid());
        var logger = new Mock<ILogger<CachingBehavior<GetCartQuery, ShoppingCartDto>>>();

        var behavior = new CachingBehavior<GetCartQuery, ShoppingCartDto>(
            cache.Object,
            currentUser.Object,
            logger.Object);

        var expected = new ShoppingCartDto { Total = 42 };

        var result = await behavior.Handle(
            new GetCartQuery(),
            _ => Task.FromResult(expected),
            CancellationToken.None);

        result.Should().BeSameAs(expected);

        // Regression test: the exclusion used to check
        // Name.Contains("ShoppingCart"), which never matched the actual
        // class name "GetCartQuery". The cart got cached for 5 minutes and
        // never invalidated by add/update/remove/clear, so customers saw a
        // stale cart after every change. The exclusion must key off the
        // feature tag (derived from the namespace), not the class name.
        cache.Verify(x => x.GetAsync<ShoppingCartDto>(It.IsAny<string>()), Times.Never);
        cache.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<ShoppingCartDto>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Not_Cache_Dashboard_Queries()
    {
        var cache = new Mock<ICacheService>();
        var currentUser = CurrentUserMock(Guid.NewGuid());
        var logger = new Mock<ILogger<CachingBehavior<GetDashboardQuery, DashboardDto>>>();

        var behavior = new CachingBehavior<GetDashboardQuery, DashboardDto>(
            cache.Object,
            currentUser.Object,
            logger.Object);

        var expected = new DashboardDto { TotalProducts = 5 };

        var result = await behavior.Handle(
            new GetDashboardQuery(),
            _ => Task.FromResult(expected),
            CancellationToken.None);

        result.Should().BeSameAs(expected);

        // Dashboard aggregates data that many unrelated write handlers touch
        // (orders, products, inventory, customers), so it is excluded from
        // caching entirely rather than relying on a "Dashboard" tag being
        // invalidated everywhere those writes happen.
        cache.Verify(x => x.GetAsync<DashboardDto>(It.IsAny<string>()), Times.Never);
        cache.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<DashboardDto>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Not_Cache_Wishlist_Queries()
    {
        var cache = new Mock<ICacheService>();
        var currentUser = CurrentUserMock(Guid.NewGuid());
        var logger = new Mock<ILogger<CachingBehavior<GetWishlistQuery, List<WishlistItemDto>>>>();

        var behavior = new CachingBehavior<GetWishlistQuery, List<WishlistItemDto>>(
            cache.Object,
            currentUser.Object,
            logger.Object);

        var expected = new List<WishlistItemDto>();

        await behavior.Handle(
            new GetWishlistQuery(),
            _ => Task.FromResult(expected),
            CancellationToken.None);

        // Found during final end-to-end testing: removing then re-adding the
        // same product looked like it silently failed, because the cached
        // wishlist from before the removal kept being served. Add/Remove
        // never invalidated a "Wishlist" tag, so — like ShoppingCart —
        // caching this only ever produced stale reads.
        cache.Verify(x => x.GetAsync<List<WishlistItemDto>>(It.IsAny<string>()), Times.Never);
        cache.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<WishlistItemDto>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Not_Cache_Loyalty_Queries()
    {
        var cache = new Mock<ICacheService>();
        var currentUser = CurrentUserMock(Guid.NewGuid());
        var logger = new Mock<ILogger<CachingBehavior<GetMyLoyaltyQuery, LoyaltyAccountDto>>>();

        var behavior = new CachingBehavior<GetMyLoyaltyQuery, LoyaltyAccountDto>(
            cache.Object,
            currentUser.Object,
            logger.Object);

        var expected = new LoyaltyAccountDto { Points = 100 };

        await behavior.Handle(
            new GetMyLoyaltyQuery(),
            _ => Task.FromResult(expected),
            CancellationToken.None);

        // Points are awarded from the Orders feature (a different tag), so
        // nothing invalidates a cached "Loyalty" entry when a delivered
        // order awards points — a customer could pay, get marked Delivered,
        // and still see stale (lower) points for 5 minutes.
        cache.Verify(x => x.GetAsync<LoyaltyAccountDto>(It.IsAny<string>()), Times.Never);
        cache.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<LoyaltyAccountDto>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Scope_Cache_Key_By_Current_User()
    {
        // GetMyOrdersQuery takes no parameters — the handler resolves "the
        // current user" itself via ICurrentUserService. If the cache key
        // doesn't also include the user, two different users hitting the
        // same endpoint collapse onto one cache entry and the second user
        // is served the first user's private order history. This
        // regression test asserts the keys differ per user.
        var cache = new Mock<ICacheService>();
        var logger = new Mock<ILogger<CachingBehavior<GetMyOrdersQuery, List<OrderDto>>>>();

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var capturedKeys = new List<string>();

        cache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<OrderDto>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string?>()))
            .Callback<string, List<OrderDto>, TimeSpan, string?>(
                (key, _, _, _) => capturedKeys.Add(key))
            .Returns(Task.CompletedTask);

        var behaviorForA = new CachingBehavior<GetMyOrdersQuery, List<OrderDto>>(
            cache.Object,
            CurrentUserMock(userA).Object,
            logger.Object);

        var behaviorForB = new CachingBehavior<GetMyOrdersQuery, List<OrderDto>>(
            cache.Object,
            CurrentUserMock(userB).Object,
            logger.Object);

        await behaviorForA.Handle(
            new GetMyOrdersQuery(),
            _ => Task.FromResult(new List<OrderDto> { new() { OrderNumber = "A's order" } }),
            CancellationToken.None);

        await behaviorForB.Handle(
            new GetMyOrdersQuery(),
            _ => Task.FromResult(new List<OrderDto> { new() { OrderNumber = "B's order" } }),
            CancellationToken.None);

        capturedKeys.Should().HaveCount(2);
        capturedKeys[0].Should().NotBe(capturedKeys[1]);
    }
}
