using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Integrity;
[Trait("Category", "Integration")]
public class CheckoutTransactionIntegrityTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private const int InitialStock = 10;

    private Guid _productId;
    private Guid _variantId;

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    public async Task InitializeAsync()
    {
        await using var context = NewContext();

        var brandId = await context.Brands.Select(x => x.Id).FirstAsync();
        var categoryId = await context.Categories.Select(x => x.Id).FirstAsync();

        var product = new Product
        {
            Name = "QA Checkout Integrity Product",
            Slug = $"qa-integrity-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by CheckoutTransactionIntegrityTests; removed in DisposeAsync.",
            BrandId = brandId,
            CategoryId = categoryId
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-INT-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = InitialStock,
            MinimumStock = 0
        };

        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        _productId = product.Id;
        _variantId = variant.Id;
    }

    public async Task DisposeAsync()
    {
        await using var context = NewContext();

        var orderItems = await context.OrderItems
            .Where(x => x.ProductVariantId == _variantId)
            .ToListAsync();

        var orderIds = orderItems.Select(x => x.OrderId).Distinct().ToList();

        context.OrderItems.RemoveRange(orderItems);
        await context.SaveChangesAsync();

        var orders = await context.Orders
            .Where(x => orderIds.Contains(x.Id))
            .ToListAsync();

        context.Orders.RemoveRange(orders);
        await context.SaveChangesAsync();

        var movements = await context.InventoryMovements
            .Where(x => x.ProductVariantId == _variantId)
            .ToListAsync();

        context.InventoryMovements.RemoveRange(movements);
        await context.SaveChangesAsync();

        var leftoverCartItems = await context.ShoppingCartItems
            .Where(x => x.ProductVariantId == _variantId)
            .ToListAsync();

        var cartIds = leftoverCartItems.Select(x => x.ShoppingCartId).Distinct().ToList();

        context.ShoppingCartItems.RemoveRange(leftoverCartItems);
        await context.SaveChangesAsync();

        var carts = await context.ShoppingCarts
            .Where(x => cartIds.Contains(x.Id))
            .ToListAsync();

        context.ShoppingCarts.RemoveRange(carts);
        await context.SaveChangesAsync();

        var outboxMessages = await context.OutboxMessages
            .Where(x => orderIds.Select(id => id.ToString()).Any(id => x.Content.Contains(id)))
            .ToListAsync();

        context.OutboxMessages.RemoveRange(outboxMessages);
        await context.SaveChangesAsync();

        var variant = await context.ProductVariants.FindAsync(_variantId);
        if (variant != null)
            context.ProductVariants.Remove(variant);

        var product = await context.Products.FindAsync(_productId);
        if (product != null)
            context.Products.Remove(product);

        await context.SaveChangesAsync();
    }

    private static async Task SeedCartAsync(ApplicationDbContext context, Guid userId, Guid variantId, int quantity)
    {
        var cart = new ShoppingCart { UserId = userId };
        context.ShoppingCarts.Add(cart);
        await context.SaveChangesAsync();

        context.ShoppingCartItems.Add(new ShoppingCartItem
        {
            ShoppingCartId = cart.Id,
            ProductVariantId = variantId,
            Quantity = quantity,
            UnitPrice = 10m
        });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Failed_Payment_After_Stock_Reservation_Rolls_Back_Everything()
    {
        var userId = Guid.NewGuid();

        await using (var seedContext = NewContext())
            await SeedCartAsync(seedContext, userId, _variantId, quantity: 7);

        var outboxCountBefore = await CountOutboxMessagesAsync();

        await using var context = NewContext();

        // Mirrors TransactionBehavior, which wraps every real Command.
        await using var transaction = await context.Database.BeginTransactionAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var payment = new Mock<IPaymentService>();
        payment
            .Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(PaymentResult.Failed("Simulated payment failure for 6.2.7"));

        var cache = new Mock<ICacheService>();
        var inventoryService = new InventoryService(context, cache.Object);

        var handler = new CheckoutCommandHandler(
            context,
            currentUser.Object,
            payment.Object,
            inventoryService,
            cache.Object,
            NullLogger<CheckoutCommandHandler>.Instance);

        var act = async () => await handler.Handle(new CheckoutCommand(), default);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Simulated payment failure for 6.2.7");

        await transaction.RollbackAsync();

        await using var verify = NewContext();

        var finalVariant = await verify.ProductVariants
            .AsNoTracking()
            .FirstAsync(x => x.Id == _variantId);

        finalVariant.Stock.Should().Be(
            InitialStock,
            "the failed payment must roll back the stock reservation made just before it, not just skip creating an order");

        var movements = await verify.InventoryMovements
            .AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId)
            .ToListAsync();

        movements.Should().BeEmpty("the InventoryMovement written inside TryRegisterExitAsync must roll back with everything else");

        var orders = await verify.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync();

        orders.Should().BeEmpty();

        var orderItems = await verify.OrderItems
            .AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId)
            .ToListAsync();

        orderItems.Should().BeEmpty();

        var outboxCountAfter = await CountOutboxMessagesAsync();
        outboxCountAfter.Should().Be(outboxCountBefore, "no OrderCreated message should have been queued for a checkout that never completed");

        var cart = await verify.ShoppingCarts
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        cart.Should().NotBeNull("the cart must survive a checkout that rolled back");
        cart!.Items.Should().ContainSingle(x => x.ProductVariantId == _variantId && x.Quantity == 7);
    }

    [Fact]
    public async Task Successful_Checkout_Leaves_Order_And_Inventory_Consistent()
    {
        var userId = Guid.NewGuid();

        await using (var seedContext = NewContext())
            await SeedCartAsync(seedContext, userId, _variantId, quantity: 7);

        await using var context = NewContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var payment = new Mock<IPaymentService>();
        payment
            .Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(PaymentResult.Succeeded("tx-627-success"));

        var cache = new Mock<ICacheService>();
        var inventoryService = new InventoryService(context, cache.Object);

        var handler = new CheckoutCommandHandler(
            context,
            currentUser.Object,
            payment.Object,
            inventoryService,
            cache.Object,
            NullLogger<CheckoutCommandHandler>.Instance);

        var orderId = await handler.Handle(new CheckoutCommand(), default);

        await transaction.CommitAsync();

        await using var verify = NewContext();

        var finalVariant = await verify.ProductVariants
            .AsNoTracking()
            .FirstAsync(x => x.Id == _variantId);

        finalVariant.Stock.Should().Be(InitialStock - 7);

        var movements = await verify.InventoryMovements
            .AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId)
            .ToListAsync();

        movements.Should().HaveCount(1);
        movements[0].Quantity.Should().Be(7);
        movements[0].IsEntry.Should().BeFalse();

        var orderItems = await verify.OrderItems
            .AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId)
            .ToListAsync();

        orderItems.Should().HaveCount(1);
        orderItems[0].Quantity.Should().Be(7);
        orderItems[0].OrderId.Should().Be(orderId);
        
        orderItems[0].Quantity.Should().Be(movements[0].Quantity);
        (InitialStock - finalVariant.Stock).Should().Be(movements[0].Quantity);
    }

    private static async Task<int> CountOutboxMessagesAsync()
    {
        await using var context = NewContext();
        return await context.OutboxMessages.CountAsync();
    }
}
