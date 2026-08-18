using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Integrity;

public class CheckoutIdempotencyReproductionTests
{
    public class SequentialRetry
    {
        private static async Task<(ApplicationDbContext Context, ProductVariant Variant, ShoppingCart Cart, Guid UserId, SqliteConnectionHolder ConnectionHolder)>
            SeedAsync()
        {
            var (context, connection) = SqliteDbContextHelper.CreateDbContext();

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
                Stock = 10
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

            return (context, variant, cart, userId, new SqliteConnectionHolder(connection));
        }

        private static (CheckoutCommandHandler Handler, TransactionBehavior<CheckoutCommand, Guid> Behavior, Mock<IPaymentService> PaymentMock)
            BuildPipeline(ApplicationDbContext context, Guid userId, PaymentResult paymentResult)
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.UserId).Returns(userId);

            var paymentMock = new Mock<IPaymentService>();
            paymentMock
                .Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(paymentResult);

            var cacheMock = new Mock<ICacheService>();
            var inventoryService = new InventoryService(context, cacheMock.Object);

            var handler = new CheckoutCommandHandler(
                context, currentUser.Object, paymentMock.Object, inventoryService,
                new ZeroShippingCostCalculator(), cacheMock.Object,
                NullLogger<CheckoutCommandHandler>.Instance);

            var behavior = new TransactionBehavior<CheckoutCommand, Guid>(
                context, NullLogger<TransactionBehavior<CheckoutCommand, Guid>>.Instance);

            return (handler, behavior, paymentMock);
        }

        [Fact]
        public async Task Retrying_Checkout_After_It_Already_Succeeded_Finds_An_Empty_Cart_And_Is_Rejected()
        {
            var (context1, variant, _, userId, connectionHolder) = await SeedAsync();
            using var _1 = connectionHolder;

            var (handler1, behavior1, paymentMock1) = BuildPipeline(context1, userId, PaymentResult.Succeeded("tx-first-attempt"));

            var firstOrderId = await behavior1.Handle(
                new CheckoutCommand(),
                _ => handler1.Handle(new CheckoutCommand(), default),
                default);

            firstOrderId.Should().NotBeEmpty();
            paymentMock1.Verify(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            var ordersAfterFirst = await context1.Orders.AsNoTracking().Where(o => o.UserId == userId).ToListAsync();
            ordersAfterFirst.Should().ContainSingle();

            var cartItemsAfterFirst = await context1.ShoppingCartItems.AsNoTracking()
                .Where(i => i.ProductVariantId == variant.Id).ToListAsync();
            cartItemsAfterFirst.Should().BeEmpty("checkout must have emptied the cart");

            await using var context2 = connectionHolder.NewContext();
            var (handler2, behavior2, paymentMock2) = BuildPipeline(context2, userId, PaymentResult.Succeeded("tx-retry-attempt"));

            var retryAct = async () => await behavior2.Handle(
                new CheckoutCommand(),
                _ => handler2.Handle(new CheckoutCommand(), default),
                default);

            await retryAct.Should().ThrowAsync<BadRequestException>(
                "the retry re-reads the cart via a fresh DbContext and finds it already emptied by the first successful checkout")
                .WithMessage("El carrito está vacío.");

            paymentMock2.Verify(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never,
                "the retry must never reach the payment call once the empty-cart guard rejects it");

            var ordersAfterRetry = await context2.Orders.AsNoTracking().Where(o => o.UserId == userId).ToListAsync();
            ordersAfterRetry.Should().ContainSingle("a sequential retry after a completed checkout must not create a second Order");

            var finalVariant = await context2.ProductVariants.AsNoTracking().FirstAsync(v => v.Id == variant.Id);
            finalVariant.Stock.Should().Be(8, "stock must reflect exactly one accepted purchase of 2, not two");
        }

        [Fact]
        public async Task Failed_Payment_After_Claiming_The_Cart_Rolls_Back_The_Claim_And_A_Retry_Can_Succeed()
        {
            var (context1, variant, _, userId, connectionHolder) = await SeedAsync();
            using var _1 = connectionHolder;

            var (handler1, behavior1, paymentMock1) = BuildPipeline(context1, userId, PaymentResult.Failed("Fondos insuficientes."));

            var failedAct = async () => await behavior1.Handle(
                new CheckoutCommand(),
                _ => handler1.Handle(new CheckoutCommand(), default),
                default);

            await failedAct.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Fondos insuficientes.");

            paymentMock1.Verify(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            await using var verifyAfterFailure = connectionHolder.NewContext();

            var cartItemsAfterFailure = await verifyAfterFailure.ShoppingCartItems.AsNoTracking()
                .Where(i => i.ProductVariantId == variant.Id).ToListAsync();
            cartItemsAfterFailure.Should().ContainSingle(
                "the early cart claim must roll back along with everything else when payment fails afterward — " +
                "the cart must NOT be left consumed for a checkout that never completed");

            var ordersAfterFailure = await verifyAfterFailure.Orders.AsNoTracking().Where(o => o.UserId == userId).ToListAsync();
            ordersAfterFailure.Should().BeEmpty("no Order must exist for a checkout whose payment failed");

            var variantAfterFailure = await verifyAfterFailure.ProductVariants.AsNoTracking().FirstAsync(v => v.Id == variant.Id);
            variantAfterFailure.Stock.Should().Be(10, "the stock reservation must also roll back — nothing was actually sold");

            await using var context2 = connectionHolder.NewContext();
            var (handler2, behavior2, paymentMock2) = BuildPipeline(context2, userId, PaymentResult.Succeeded("tx-retry-success"));

            var retryOrderId = await behavior2.Handle(
                new CheckoutCommand(),
                _ => handler2.Handle(new CheckoutCommand(), default),
                default);

            retryOrderId.Should().NotBeEmpty();
            paymentMock2.Verify(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            await using var verifyAfterRetry = connectionHolder.NewContext();

            var ordersAfterRetry = await verifyAfterRetry.Orders.AsNoTracking().Where(o => o.UserId == userId).ToListAsync();
            ordersAfterRetry.Should().ContainSingle("the retry must succeed and create exactly one Order");

            var cartItemsAfterRetry = await verifyAfterRetry.ShoppingCartItems.AsNoTracking()
                .Where(i => i.ProductVariantId == variant.Id).ToListAsync();
            cartItemsAfterRetry.Should().BeEmpty("the successful retry must consume the cart");

            var variantAfterRetry = await verifyAfterRetry.ProductVariants.AsNoTracking().FirstAsync(v => v.Id == variant.Id);
            variantAfterRetry.Stock.Should().Be(8, "10 - 2: the retry's single purchase must be the only one that actually persists");
        }
    }

    public sealed class SqliteConnectionHolder : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

        public SqliteConnectionHolder(Microsoft.Data.Sqlite.SqliteConnection connection) => _connection = connection;

        public ApplicationDbContext NewContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options, null);

        public void Dispose() => _connection.Dispose();
    }

    [Trait("Category", "Integration")]
    public class ConcurrentDoubleClick : IAsyncLifetime
    {
        private const string ConnectionString =
            "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

        private Guid _productId;
        private Guid _variantId;

        private static ApplicationDbContext NewContext() =>
            new(
                new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options,
                null);

        public async Task InitializeAsync()
        {
            await using var context = NewContext();

            var brandId = await context.Brands.Select(x => x.Id).FirstAsync();
            var categoryId = await context.Categories.Select(x => x.Id).FirstAsync();

            var product = new Product
            {
                Name = "QA Idempotency Race Product",
                Slug = $"qa-idempotency-{Guid.NewGuid():N}",
                ShortDescription = "QA test product, safe to delete.",
                Description = "Created by CheckoutIdempotencyReproductionTests; removed in DisposeAsync.",
                BrandId = brandId,
                CategoryId = categoryId
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = product.Id,
                SKU = $"QA-IDEM-{Guid.NewGuid():N}"[..20],
                Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
                Price = 10m,
                Stock = 1000,
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

            var orderItems = await context.OrderItems.Where(x => x.ProductVariantId == _variantId).ToListAsync();
            var orderIds = orderItems.Select(x => x.OrderId).Distinct().ToList();
            context.OrderItems.RemoveRange(orderItems);
            await context.SaveChangesAsync();

            var orders = await context.Orders.Where(x => orderIds.Contains(x.Id)).ToListAsync();
            context.Orders.RemoveRange(orders);
            await context.SaveChangesAsync();

            await context.InventoryMovements.Where(x => x.ProductVariantId == _variantId).ExecuteDeleteAsync();

            var leftoverCartItems = await context.ShoppingCartItems.Where(x => x.ProductVariantId == _variantId).ToListAsync();
            var cartIds = leftoverCartItems.Select(x => x.ShoppingCartId).Distinct().ToList();
            context.ShoppingCartItems.RemoveRange(leftoverCartItems);
            await context.SaveChangesAsync();

            var carts = await context.ShoppingCarts.Where(x => cartIds.Contains(x.Id)).ToListAsync();
            context.ShoppingCarts.RemoveRange(carts);
            await context.SaveChangesAsync();

            var outboxMessages = await context.OutboxMessages
                .Where(x => orderIds.Select(id => id.ToString()).Any(id => x.Content.Contains(id)))
                .ToListAsync();
            context.OutboxMessages.RemoveRange(outboxMessages);
            await context.SaveChangesAsync();

            var variant = await context.ProductVariants.FindAsync(_variantId);
            if (variant != null) context.ProductVariants.Remove(variant);

            var product = await context.Products.FindAsync(_productId);
            if (product != null) context.Products.Remove(product);

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
                Quantity = 2,
                UnitPrice = 10m
            });
            await context.SaveChangesAsync();
        }

        private enum CheckoutOutcome
        {
            Succeeded,
            RejectedCleanly,
            RejectedWithUnhandledConcurrencyException
        }

        private static async Task<(CheckoutOutcome Outcome, Guid? OrderId, bool PaymentWasCalled, string? Message)> RunCheckoutAsync(
            ApplicationDbContext context, Guid userId)
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.UserId).Returns(userId);

            var payment = new Mock<IPaymentService>();
            payment
                .Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(PaymentResult.Succeeded($"tx-{Guid.NewGuid():N}"));

            var cache = new Mock<ICacheService>();
            var inventoryService = new InventoryService(context, cache.Object);

            var handler = new CheckoutCommandHandler(
                context, currentUser.Object, payment.Object, inventoryService,
                new ZeroShippingCostCalculator(), cache.Object,
                NullLogger<CheckoutCommandHandler>.Instance);

            var transactionBehavior = new TransactionBehavior<CheckoutCommand, Guid>(
                context, NullLogger<TransactionBehavior<CheckoutCommand, Guid>>.Instance);

            try
            {
                var orderId = await transactionBehavior.Handle(
                    new CheckoutCommand(),
                    _ => handler.Handle(new CheckoutCommand(), default),
                    default);
                return (CheckoutOutcome.Succeeded, orderId, PaymentWasCalled(payment), null);
            }
            catch (BadRequestException ex)
            {
                return (CheckoutOutcome.RejectedCleanly, null, PaymentWasCalled(payment), ex.Message);
            }
            catch (DbUpdateConcurrencyException)
            {
                return (CheckoutOutcome.RejectedWithUnhandledConcurrencyException, null, PaymentWasCalled(payment), null);
            }
        }

        private static bool PaymentWasCalled(Mock<IPaymentService> payment) =>
            payment.Invocations.Any(i => i.Method.Name == nameof(IPaymentService.CreatePaymentAsync));

        [Fact]
        public async Task Two_Near_Simultaneous_Checkouts_By_The_Same_User_For_The_Same_Cart()
        {
            var userId = Guid.NewGuid();

            await using (var seedContext = NewContext())
                await SeedCartAsync(seedContext, userId, _variantId);

            await using var contextA = NewContext();
            await using var contextB = NewContext();

            var taskA = RunCheckoutAsync(contextA, userId);
            var taskB = RunCheckoutAsync(contextB, userId);

            var results = await Task.WhenAll(taskA, taskB);

            await using var verify = NewContext();

            var ordersForUser = await verify.Orders.AsNoTracking().Where(o => o.UserId == userId).ToListAsync();
            var orderItemsForVariant = await verify.OrderItems.AsNoTracking()
                .Where(x => x.ProductVariantId == _variantId).ToListAsync();
            var movementsForVariant = await verify.InventoryMovements.AsNoTracking()
                .Where(x => x.ProductVariantId == _variantId).ToListAsync();
            var finalVariant = await verify.ProductVariants.AsNoTracking().FirstAsync(v => v.Id == _variantId);
            var winners = results.Count(r => r.Outcome == CheckoutOutcome.Succeeded);
            var cleanRejections = results.Count(r => r.Outcome == CheckoutOutcome.RejectedCleanly);
            var unhandledConcurrencyFailures = results.Count(r => r.Outcome == CheckoutOutcome.RejectedWithUnhandledConcurrencyException);

            winners.Should().Be(1, "exactly one of the two racing attempts must persist an Order");
            cleanRejections.Should().Be(1,
                "the loser must now be rejected cleanly by the early cart claim, before ever reaching stock or payment");
            unhandledConcurrencyFailures.Should().Be(0,
                "the pre-7.5.3 failure mode (unhandled DbUpdateConcurrencyException reaching the client as a raw 500) must no longer occur");

            var loserMessage = results.Single(r => r.Outcome == CheckoutOutcome.RejectedCleanly).Message;
            loserMessage.Should().BeOneOf("Este carrito ya fue procesado.", "El carrito está vacío.");

            results.Count(r => r.PaymentWasCalled).Should().Be(1,
                "only the winner may ever reach IPaymentService — the loser must be rejected before payment, eliminating the double-charge risk");

            ordersForUser.Should().ContainSingle("only the winner's Order is ever created");
            orderItemsForVariant.Sum(x => x.Quantity).Should().Be(2, "only the winner's 2 units are ever sold");
            movementsForVariant.Should().ContainSingle(
                "the loser must be rejected before InventoryService is ever called — no stock movement for the loser at all");
            finalVariant.Stock.Should().Be(998, "1000 - 2: only the winner's purchase touches stock");
        }
    }
}
