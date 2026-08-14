using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Application.Features.Orders.DTOs;
using BeautyCommerce.Application.Features.Orders.Queries.GetAllOrders;
using BeautyCommerce.Application.Features.Orders.Queries.GetMyOrders;
using BeautyCommerce.Application.Features.Users.DTOs;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Cache;

public class CheckoutOrdersCacheStalenessTests
{
    [Fact]
    public async Task Checkout_Invalidates_Orders_So_GetMyOrders_Immediately_Shows_The_New_Order()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Product",
            Slug = $"qa-{Guid.NewGuid():N}",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 5
        };
        context.ProductVariants.Add(variant);

        var userId = Guid.NewGuid();

        var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = userId };
        cart.Items.Add(new ShoppingCartItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            Quantity = 2,
            UnitPrice = variant.Price
        });
        context.ShoppingCarts.Add(cart);

        await context.SaveChangesAsync();

        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(userId);

        var behavior = new CachingBehavior<GetMyOrdersQuery, List<OrderDto>>(
            cache,
            currentUserMock.Object,
            NullLogger<CachingBehavior<GetMyOrdersQuery, List<OrderDto>>>.Instance);

        var queryHandler = new GetMyOrdersQueryHandler(context, currentUserMock.Object);

        Task<List<OrderDto>> RunGetMyOrders() =>
            behavior.Handle(
                new GetMyOrdersQuery(),
                _ => queryHandler.Handle(new GetMyOrdersQuery(), default),
                default);

        // MISS -> empty order history is cached for this user.
        var beforeCheckout = await RunGetMyOrders();
        beforeCheckout.Should().BeEmpty("the user has not placed any order yet");

        // HIT -> confirmed cached via reference equality.
        var beforeCheckoutAgain = await RunGetMyOrders();
        ReferenceEquals(beforeCheckout, beforeCheckoutAgain).Should()
            .BeTrue("the empty result must already be served from cache before checkout runs");

        var paymentMock = new Mock<IPaymentService>();
        paymentMock
            .Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(PaymentResult.Succeeded("tx-cache-fix"));

        var inventoryService = new InventoryService(context, cache);

        var checkoutHandler = new CheckoutCommandHandler(
            context,
            currentUserMock.Object,
            paymentMock.Object,
            inventoryService,
            cache,
            NullLogger<CheckoutCommandHandler>.Instance);

        var orderId = await checkoutHandler.Handle(new CheckoutCommand(), default);
        orderId.Should().NotBeEmpty();

        // Fix under test: checkout must invalidate "Orders", forcing the next
        // GetMyOrders call to be a fresh MISS that includes the new order,
        // not the stale pre-checkout instance.
        var afterCheckout = await RunGetMyOrders();
        ReferenceEquals(beforeCheckout, afterCheckout).Should()
            .BeFalse("checkout must invalidate the Orders tag, forcing a new instance instead of reusing the stale one");
        afterCheckout.Should().ContainSingle(o => o.Id == orderId,
            "the order the user just paid for must be visible immediately, not after up to 5 minutes");
    }

    [Fact]
    public async Task Checkout_Invalidates_Orders_So_GetAllOrders_Immediately_Shows_The_New_Order_To_Admin()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Product",
            Slug = $"qa-{Guid.NewGuid():N}",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 5
        };
        context.ProductVariants.Add(variant);

        var buyerId = Guid.NewGuid();

        var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = buyerId };
        cart.Items.Add(new ShoppingCartItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            Quantity = 1,
            UnitPrice = variant.Price
        });
        context.ShoppingCarts.Add(cart);

        await context.SaveChangesAsync();

        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));

        // Admin views GetAllOrders under their own identity — a different
        // user than the one about to check out, which is the realistic case.
        var adminId = Guid.NewGuid();
        var adminUserMock = new Mock<ICurrentUserService>();
        adminUserMock.Setup(x => x.UserId).Returns(adminId);

        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var behavior = new CachingBehavior<GetAllOrdersQuery, List<OrderDto>>(
            cache,
            adminUserMock.Object,
            NullLogger<CachingBehavior<GetAllOrdersQuery, List<OrderDto>>>.Instance);

        var queryHandler = new GetAllOrdersQueryHandler(context, userServiceMock.Object);

        Task<List<OrderDto>> RunGetAllOrders() =>
            behavior.Handle(
                new GetAllOrdersQuery(),
                _ => queryHandler.Handle(new GetAllOrdersQuery(), default),
                default);

        // MISS -> empty order list cached for the admin.
        var beforeCheckout = await RunGetAllOrders();
        beforeCheckout.Should().BeEmpty("no order has been placed yet");

        // HIT -> confirmed cached via reference equality.
        var beforeCheckoutAgain = await RunGetAllOrders();
        ReferenceEquals(beforeCheckout, beforeCheckoutAgain).Should()
            .BeTrue("the empty admin list must already be served from cache before checkout runs");

        var buyerMock = new Mock<ICurrentUserService>();
        buyerMock.Setup(x => x.UserId).Returns(buyerId);

        var paymentMock = new Mock<IPaymentService>();
        paymentMock
            .Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(PaymentResult.Succeeded("tx-cache-fix-admin"));

        var inventoryService = new InventoryService(context, cache);

        var checkoutHandler = new CheckoutCommandHandler(
            context,
            buyerMock.Object,
            paymentMock.Object,
            inventoryService,
            cache,
            NullLogger<CheckoutCommandHandler>.Instance);

        var orderId = await checkoutHandler.Handle(new CheckoutCommand(), default);
        orderId.Should().NotBeEmpty();

        // Fix under test: a single InvalidateTagAsync("Orders") reaches
        // GetAllOrders too, since it shares the same feature tag as
        // GetMyOrders/GetOrderById/GetAdminOrderById.
        var afterCheckout = await RunGetAllOrders();
        ReferenceEquals(beforeCheckout, afterCheckout).Should()
            .BeFalse("checkout by any user must invalidate the shared Orders tag the admin's cache also relies on");
        afterCheckout.Should().ContainSingle(o => o.Id == orderId,
            "the admin must see a newly placed order immediately, without waiting for the cache to expire");
    }
}
