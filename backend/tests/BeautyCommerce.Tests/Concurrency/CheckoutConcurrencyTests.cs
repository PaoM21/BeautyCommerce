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

namespace BeautyCommerce.Tests.Concurrency;

[Trait("Category", "Integration")]
public class CheckoutConcurrencyTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

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
            Name = "QA Checkout Race Product",
            Slug = $"qa-checkout-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by CheckoutConcurrencyTests; removed in DisposeAsync.",
            BrandId = brandId,
            CategoryId = categoryId
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-CHK-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 7,
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

    private static async Task SeedCartAsync(ApplicationDbContext context, Guid userId, Guid variantId)
    {
        var cart = new ShoppingCart { UserId = userId };
        context.ShoppingCarts.Add(cart);
        await context.SaveChangesAsync();

        context.ShoppingCartItems.Add(new ShoppingCartItem
        {
            ShoppingCartId = cart.Id,
            ProductVariantId = variantId,
            Quantity = 7,
            UnitPrice = 10m
        });

        await context.SaveChangesAsync();
    }

    private static async Task<Guid?> RunCheckout(ApplicationDbContext context, Guid userId)
    {
        // Same wrapping TransactionBehavior applies to every real Command.
        await using var transaction = await context.Database.BeginTransactionAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var payment = new Mock<IPaymentService>();
        payment
            .Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(PaymentResult.Succeeded($"tx-{userId:N}"));

        var cache = new Mock<ICacheService>();
        var inventoryService = new InventoryService(context, cache.Object);

        var handler = new CheckoutCommandHandler(
            context,
            currentUser.Object,
            payment.Object,
            inventoryService,
            cache.Object,
            NullLogger<CheckoutCommandHandler>.Instance);

        try
        {
            var orderId = await handler.Handle(new CheckoutCommand(), default);
            await transaction.CommitAsync();
            return orderId;
        }
        catch (BadRequestException)
        {
            // The correct, expected outcome for the loser of the race:
            // TryRegisterExitAsync's atomic UPDATE affected 0 rows, so
            // CheckoutCommandHandler throws before ever charging this
            // customer. Nothing was written for this context, but roll
            // back explicitly for clarity.
            await transaction.RollbackAsync();
            return null;
        }
    }

    [Fact]
    public async Task Concurrent_Checkouts_Never_Both_Succeed()
    {
        const int rounds = 10;

        for (var round = 1; round <= rounds; round++)
        {
            await using (var reset = NewContext())
            {
                var roundOrderItems = await reset.OrderItems
                    .Where(x => x.ProductVariantId == _variantId)
                    .ToListAsync();

                var roundOrderIds = roundOrderItems.Select(x => x.OrderId).Distinct().ToList();

                reset.OrderItems.RemoveRange(roundOrderItems);
                await reset.SaveChangesAsync();

                var roundOrders = await reset.Orders
                    .Where(x => roundOrderIds.Contains(x.Id))
                    .ToListAsync();

                reset.Orders.RemoveRange(roundOrders);
                await reset.SaveChangesAsync();

                await reset.InventoryMovements
                    .Where(x => x.ProductVariantId == _variantId)
                    .ExecuteDeleteAsync();

                await reset.ProductVariants
                    .Where(x => x.Id == _variantId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Stock, 7));
            }

            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();

            await using (var seedContext = NewContext())
                await SeedCartAsync(seedContext, userA, _variantId);

            await using (var seedContext = NewContext())
                await SeedCartAsync(seedContext, userB, _variantId);

            await using var contextA = NewContext();
            await using var contextB = NewContext();

            var taskA = RunCheckout(contextA, userA);
            var taskB = RunCheckout(contextB, userB);

            var results = await Task.WhenAll(taskA, taskB);

            var successCount = results.Count(x => x != null);

            await using var verify = NewContext();

            var finalVariant = await verify.ProductVariants
                .AsNoTracking()
                .FirstAsync(x => x.Id == _variantId);

            var orderItems = await verify.OrderItems
                .AsNoTracking()
                .Where(x => x.ProductVariantId == _variantId)
                .ToListAsync();

            var movements = await verify.InventoryMovements
                .AsNoTracking()
                .Where(x => x.ProductVariantId == _variantId)
                .ToListAsync();

            successCount.Should().Be(1, $"round {round}: exactly one checkout should be accepted");
            finalVariant.Stock.Should().Be(0, $"round {round}: stock must reflect exactly one accepted purchase of 7");
            orderItems.Should().HaveCount(1, $"round {round}: only the accepted checkout should have an OrderItem");
            orderItems.Sum(x => x.Quantity).Should().Be(7, $"round {round}");
            movements.Should().HaveCount(1, $"round {round}: only the accepted checkout should leave a movement");
            movements.Sum(x => x.Quantity).Should().Be(7, $"round {round}: movement history must match the single accepted purchase");
        }
    }
}
