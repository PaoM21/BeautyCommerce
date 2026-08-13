using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.Queries.GetMyOrders;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Cache;

// 6.3.7: formal proof that per-user, zero-parameter cached queries never
// leak between users. GetMyOrdersQuery is the only currently-cached query
// in this shape (GetWishlistQuery/GetMyLoyaltyQuery/GetLoyaltyHistoryQuery/
// GetCartQuery are excluded from caching entirely — see CachingBehavior —
// so there is no cache for them to leak from; the isolation guarantee
// there is "nothing is ever shared" rather than "the key is scoped
// correctly"). Uses a real MemoryCacheService, not a mock, so this is
// exercising the actual key-construction code, not a description of it.
public class UserIsolationTests
{
    private static CachingBehavior<GetMyOrdersQuery, List<BeautyCommerce.Application.Features.Orders.DTOs.OrderDto>> NewBehavior(
        ICacheService cache, Guid userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        return new CachingBehavior<GetMyOrdersQuery, List<BeautyCommerce.Application.Features.Orders.DTOs.OrderDto>>(
            cache,
            currentUser.Object,
            NullLogger<CachingBehavior<GetMyOrdersQuery, List<BeautyCommerce.Application.Features.Orders.DTOs.OrderDto>>>.Instance);
    }

    [Fact]
    public async Task Two_Users_Never_See_Each_Others_Cached_Orders()
    {
        // One shared cache (as the real MemoryCache singleton is shared
        // across every request in production), five independent
        // user pairs, each with deliberately different order numbers, to
        // make sure isolation isn't a coincidence of a single run.
        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));

        for (var round = 1; round <= 5; round++)
        {
            var (context, connection) = SqliteDbContextHelper.CreateDbContext();
            using var _ = connection;

            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();

            context.Orders.Add(new Order
            {
                UserId = userA,
                OrderNumber = $"ORD-A-{round}",
                Status = BeautyCommerce.Domain.Enums.OrderStatus.Paid,
                Total = 100m + round,
                OrderDate = DateTime.UtcNow
            });

            context.Orders.Add(new Order
            {
                UserId = userB,
                OrderNumber = $"ORD-B-{round}",
                Status = BeautyCommerce.Domain.Enums.OrderStatus.Paid,
                Total = 200m + round,
                OrderDate = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var queryHandlerForA = new GetMyOrdersQueryHandler(
                context, Mock.Of<ICurrentUserService>(x => x.UserId == userA));
            var behaviorForA = NewBehavior(cache, userA);

            var queryHandlerForB = new GetMyOrdersQueryHandler(
                context, Mock.Of<ICurrentUserService>(x => x.UserId == userB));
            var behaviorForB = NewBehavior(cache, userB);

            // A: MISS -> caches A's own order.
            var aFirst = await behaviorForA.Handle(
                new GetMyOrdersQuery(), _ => queryHandlerForA.Handle(new GetMyOrdersQuery(), default), default);

            aFirst.Should().ContainSingle();
            aFirst[0].OrderNumber.Should().Be($"ORD-A-{round}");

            // A: HIT -> the exact same cached instance.
            var aSecond = await behaviorForA.Handle(
                new GetMyOrdersQuery(), _ => queryHandlerForA.Handle(new GetMyOrdersQuery(), default), default);

            ReferenceEquals(aFirst, aSecond).Should().BeTrue($"round {round}: A's second call must be a cache hit");

            // B: MISS -> a DIFFERENT cache entry (different user-scoped
            // key), never A's cached instance, never A's data.
            var bFirst = await behaviorForB.Handle(
                new GetMyOrdersQuery(), _ => queryHandlerForB.Handle(new GetMyOrdersQuery(), default), default);

            bFirst.Should().ContainSingle();
            bFirst[0].OrderNumber.Should().Be($"ORD-B-{round}", $"round {round}: B must never see A's order");
            ReferenceEquals(aFirst, bFirst).Should().BeFalse($"round {round}: A and B must never share a cache entry");

            // B: HIT -> the exact same cached instance as B's own first call.
            var bSecond = await behaviorForB.Handle(
                new GetMyOrdersQuery(), _ => queryHandlerForB.Handle(new GetMyOrdersQuery(), default), default);

            ReferenceEquals(bFirst, bSecond).Should().BeTrue($"round {round}: B's second call must be a cache hit");
            bSecond[0].OrderNumber.Should().Be($"ORD-B-{round}");

            // One more read from A, after B has been cached, to make sure
            // B's cache entry didn't retroactively contaminate A's.
            var aThird = await behaviorForA.Handle(
                new GetMyOrdersQuery(), _ => queryHandlerForA.Handle(new GetMyOrdersQuery(), default), default);

            aThird[0].OrderNumber.Should().Be($"ORD-A-{round}");
            ReferenceEquals(aFirst, aThird).Should().BeTrue($"round {round}: A's cache entry must be unaffected by B's traffic");
        }
    }
}
