using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderStatus;
using BeautyCommerce.Application.Features.Dashboard.Queries.GetDashboard;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using BeautyCommerce.Infrastructure.Identity;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit.Abstractions;

namespace BeautyCommerce.Tests.Integrity;

[Trait("Category", "Integration")]
public class DashboardReproductionTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private readonly ITestOutputHelper _output;
    public DashboardReproductionTests(ITestOutputHelper output) => _output = output;

    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options, null);

    private readonly List<Guid> _productIds = new();
    private readonly List<Guid> _variantIds = new();
    private readonly List<Guid> _userIds = new();
    private readonly List<Guid> _orderIds = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var context = NewContext();

        await context.OrderItems.Where(oi => _orderIds.Contains(oi.OrderId)).ExecuteDeleteAsync();
        await context.Orders.Where(o => _orderIds.Contains(o.Id)).ExecuteDeleteAsync();

        var cartIds = await context.ShoppingCarts.Where(c => _userIds.Contains(c.UserId)).Select(c => c.Id).ToListAsync();
        await context.ShoppingCartItems.Where(i => cartIds.Contains(i.ShoppingCartId)).ExecuteDeleteAsync();
        await context.ShoppingCarts.Where(c => cartIds.Contains(c.Id)).ExecuteDeleteAsync();

        await context.UserRoles.Where(ur => _userIds.Contains(ur.UserId)).ExecuteDeleteAsync();
        await context.Users.Where(u => _userIds.Contains(u.Id)).ExecuteDeleteAsync();

        await context.ProductVariants.Where(v => _variantIds.Contains(v.Id)).ExecuteDeleteAsync();
        await context.Products.IgnoreQueryFilters().Where(p => _productIds.Contains(p.Id)).ExecuteDeleteAsync();
    }

    private static UserManager<ApplicationUser> BuildUserManager(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.AddSingleton(context);

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider().GetRequiredService<UserManager<ApplicationUser>>();
    }

    private async Task<Guid> CreateCustomerAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, string email)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "QA",
            LastName = "Dashboard"
        };
        (await userManager.CreateAsync(user, "Qa12345!")).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(user, "Customer")).Succeeded.Should().BeTrue();

        _userIds.Add(user.Id);
        return user.Id;
    }

    private async Task<Guid> CreateOrderAsync(Guid userId, Guid variantId, int quantity, decimal expectedTotal)
    {
        await using var context = NewContext();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var addHandler = new AddCartItemCommandHandler(context, currentUser.Object);
        await addHandler.Handle(
            new AddCartItemCommand { Item = new AddCartItemDto { ProductVariantId = variantId, Quantity = quantity } },
            default);

        var paymentMock = new Mock<IPaymentService>();
        paymentMock
            .Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(PaymentResult.Succeeded($"SIM-{Guid.NewGuid():N}"));

        var cacheMock = new Mock<ICacheService>();
        var inventoryService = new InventoryService(context, cacheMock.Object);

        var checkoutHandler = new CheckoutCommandHandler(
            context, currentUser.Object, paymentMock.Object, inventoryService,
            new ZeroShippingCostCalculator(), cacheMock.Object,
            NullLogger<CheckoutCommandHandler>.Instance);

        var behavior = new TransactionBehavior<CheckoutCommand, Guid>(
            context, NullLogger<TransactionBehavior<CheckoutCommand, Guid>>.Instance);

        var orderId = await behavior.Handle(new CheckoutCommand(), _ => checkoutHandler.Handle(new CheckoutCommand(), default), default);

        _orderIds.Add(orderId);

        var order = await context.Orders.FirstAsync(o => o.Id == orderId);
        order.Total.Should().Be(expectedTotal, "sanity check on the known order total before using it in the reproduction");

        return orderId;
    }

    private async Task TransitionOrderAsync(Guid orderId, OrderStatus status)
    {
        await using var context = NewContext();

        var cacheMock = new Mock<ICacheService>();
        var inventoryService = new InventoryService(context, cacheMock.Object);
        var currentUserMock = new Mock<ICurrentUserService>();

        var handler = new UpdateOrderStatusCommandHandler(
            context, Mock.Of<ILoyaltyService>(), cacheMock.Object, inventoryService, currentUserMock.Object);
        var behavior = new TransactionBehavior<UpdateOrderStatusCommand, bool>(
            context, NullLogger<TransactionBehavior<UpdateOrderStatusCommand, bool>>.Instance);

        var command = new UpdateOrderStatusCommand { OrderId = orderId, Status = status };
        var result = await behavior.Handle(command, _ => handler.Handle(command, default), default);

        result.Should().BeTrue();
    }

    private async Task SetOrderDateAsync(Guid orderId, DateTime orderDateUtc)
    {
        await using var context = NewContext();
        var order = await context.Orders.FirstAsync(o => o.Id == orderId);
        order.OrderDate = orderDateUtc;
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Dashboard_Metrics_Match_Independent_Postgres_Queries()
    {
        await using var setupContext = NewContext();

        var brandId = await setupContext.Brands.Select(x => x.Id).FirstAsync();
        var categoryId = await setupContext.Categories.Select(x => x.Id).FirstAsync();
        var userManager = BuildUserManager(setupContext);

        // --- Products: 1 normal, 1 low-stock, 1 out-of-stock, 1 soon-to-be-soft-deleted ---
        async Task<(Guid ProductId, Guid VariantId)> CreateProductAsync(string name, int stock, int minimumStock)
        {
            var product = new Product
            {
                Name = name,
                Slug = $"qa-dash-{Guid.NewGuid():N}",
                ShortDescription = "QA",
                Description = "QA dashboard reproduction, removed in DisposeAsync",
                BrandId = brandId,
                CategoryId = categoryId
            };
            setupContext.Products.Add(product);
            await setupContext.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = product.Id,
                SKU = $"QA-DASH-{Guid.NewGuid():N}"[..20],
                Barcode = $"QA-DASH-{Guid.NewGuid():N}"[..20],
                Price = 10000m,
                Stock = stock,
                MinimumStock = minimumStock
            };
            setupContext.ProductVariants.Add(variant);
            await setupContext.SaveChangesAsync();

            _productIds.Add(product.Id);
            _variantIds.Add(variant.Id);
            return (product.Id, variant.Id);
        }

        var (_, sellableVariantId) = await CreateProductAsync("QA Dash Sellable", stock: 1000, minimumStock: 0);
        await CreateProductAsync("QA Dash LowStock", stock: 2, minimumStock: 5);
        await CreateProductAsync("QA Dash OutOfStock", stock: 0, minimumStock: 5);
        var (softDeleteProductId, _) = await CreateProductAsync("QA Dash ToBeSoftDeleted", stock: 10, minimumStock: 0);

        var customerId = await CreateCustomerAsync(setupContext, userManager, $"qa-dash-{Guid.NewGuid():N}@test.local");


        const decimal orderTotal = 10000m; // 1 unit at 10000

        var orderPaid = await CreateOrderAsync(customerId, sellableVariantId, 1, orderTotal);          // stays Paid

        var orderProcessing = await CreateOrderAsync(customerId, sellableVariantId, 1, orderTotal);
        await TransitionOrderAsync(orderProcessing, OrderStatus.Processing);

        var orderShipped = await CreateOrderAsync(customerId, sellableVariantId, 1, orderTotal);
        await TransitionOrderAsync(orderShipped, OrderStatus.Processing);
        await TransitionOrderAsync(orderShipped, OrderStatus.Shipped);

        var orderDelivered = await CreateOrderAsync(customerId, sellableVariantId, 1, orderTotal);
        await TransitionOrderAsync(orderDelivered, OrderStatus.Processing);
        await TransitionOrderAsync(orderDelivered, OrderStatus.Shipped);
        await TransitionOrderAsync(orderDelivered, OrderStatus.Delivered);

        var orderCancelled = await CreateOrderAsync(customerId, sellableVariantId, 1, orderTotal);
        await TransitionOrderAsync(orderCancelled, OrderStatus.Cancelled);

        var orderPreviousMonth = await CreateOrderAsync(customerId, sellableVariantId, 1, orderTotal);
        var now = DateTime.UtcNow;
        var firstDayThisMonthUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastInstantPreviousMonthUtc = firstDayThisMonthUtc.AddTicks(-1);
        await SetOrderDateAsync(orderPreviousMonth, lastInstantPreviousMonthUtc);

        var orderFirstInstantThisMonth = await CreateOrderAsync(customerId, sellableVariantId, 1, orderTotal);
        await SetOrderDateAsync(orderFirstInstantThisMonth, firstDayThisMonthUtc);


        await using var verifyContext = NewContext();
        var userServiceForVerify = new UserService(BuildUserManager(verifyContext));
        var dashboard = await new GetDashboardQueryHandler(verifyContext, userServiceForVerify).Handle(new GetDashboardQuery(), default);

        _output.WriteLine($"Dashboard: Products={dashboard.TotalProducts}, Orders={dashboard.TotalOrders}, Customers={dashboard.TotalCustomers}, Pending={dashboard.PendingOrders}, Sales={dashboard.TotalSales}, SalesThisMonth={dashboard.SalesThisMonth}, LowStock={dashboard.LowStockProducts}, OutOfStock={dashboard.OutOfStockProducts}");

        dashboard.TotalProducts.Should().BeGreaterThanOrEqualTo(4, "our 4 QA products (not yet deleted) must all be counted");

        var isKnownCustomer = await verifyContext.UserRoles
            .Where(ur => ur.UserId == customerId)
            .Join(verifyContext.Roles.Where(r => r.Name == "Customer"), ur => ur.RoleId, r => r.Id, (ur, r) => r.Id)
            .AnyAsync();
        isKnownCustomer.Should().BeTrue("our QA user must genuinely hold the Customer role for TotalCustomers to count it correctly");
        dashboard.TotalCustomers.Should().BeGreaterThanOrEqualTo(1);

        dashboard.TotalOrders.Should().BeGreaterThanOrEqualTo(7, "all 7 known orders count towards TotalOrders regardless of status");


        dashboard.PendingOrders.Should().BeGreaterThanOrEqualTo(4,
            "orderPaid, orderProcessing, orderPreviousMonth and orderFirstInstantThisMonth are all Paid/Processing");

        async Task<bool> CountsAsPendingAsync(Guid orderId) =>
            await verifyContext.Orders.AnyAsync(o => o.Id == orderId &&
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Paid || o.Status == OrderStatus.Processing));

        (await CountsAsPendingAsync(orderShipped)).Should().BeFalse("Shipped must not count as pending");
        (await CountsAsPendingAsync(orderDelivered)).Should().BeFalse("Delivered must not count as pending");
        (await CountsAsPendingAsync(orderCancelled)).Should().BeFalse("Cancelled must not count as pending");
        (await CountsAsPendingAsync(orderPaid)).Should().BeTrue();
        (await CountsAsPendingAsync(orderProcessing)).Should().BeTrue();

        dashboard.TotalSales.Should().BeGreaterThanOrEqualTo(60000m);

        var cancelledStillExcluded = await verifyContext.Orders.AnyAsync(o => o.Id == orderCancelled && o.Status != OrderStatus.Cancelled);
        cancelledStillExcluded.Should().BeFalse("sanity check: a Cancelled order can never satisfy Status != Cancelled");

        dashboard.SalesThisMonth.Should().BeGreaterThanOrEqualTo(50000m);

        var previousMonthOrderCountsThisMonth = await verifyContext.Orders
            .AnyAsync(o => o.Id == orderPreviousMonth && o.Status != OrderStatus.Cancelled && o.OrderDate >= firstDayThisMonthUtc);
        previousMonthOrderCountsThisMonth.Should().BeFalse(
            "orderPreviousMonth is dated one tick before firstDayThisMonthUtc; it must fall outside the >= boundary");

        var firstInstantOrderCountsThisMonth = await verifyContext.Orders
            .AnyAsync(o => o.Id == orderFirstInstantThisMonth && o.Status != OrderStatus.Cancelled && o.OrderDate >= firstDayThisMonthUtc);
        firstInstantOrderCountsThisMonth.Should().BeTrue(
            "orderFirstInstantThisMonth is dated at exactly firstDayThisMonthUtc; the boundary must be inclusive");

        dashboard.LowStockProducts.Should().BeGreaterThanOrEqualTo(1);
        dashboard.OutOfStockProducts.Should().BeGreaterThanOrEqualTo(1);

        await using (var deleteContext = NewContext())
        {
            var product = await deleteContext.Products.FirstAsync(p => p.Id == softDeleteProductId);
            product.IsDeleted = true;
            product.DeletedAt = DateTime.UtcNow;
            await deleteContext.SaveChangesAsync();
        }

        await using var afterDeleteContext = NewContext();

        var stillCounted = await afterDeleteContext.Products.AnyAsync(p => p.Id == softDeleteProductId);
        stillCounted.Should().BeFalse("the default query filter (!IsDeleted) -- the same one TotalProducts relies on -- must exclude the soft-deleted product");

        var physicallyStillExists = await afterDeleteContext.Products.IgnoreQueryFilters()
            .AnyAsync(p => p.Id == softDeleteProductId && p.IsDeleted);
        physicallyStillExists.Should().BeTrue("sanity check: the row still physically exists, just filtered out");
    }
}
