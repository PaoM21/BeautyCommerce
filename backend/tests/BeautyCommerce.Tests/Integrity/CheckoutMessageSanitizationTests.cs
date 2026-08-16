using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeautyCommerce.Tests.Integrity;

public class CheckoutMessageSanitizationTests
{
    private class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public async Task Insufficient_Stock_Returns_Generic_Message_But_Logs_The_Real_Id()
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
            Stock = 1
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        var userId = Guid.NewGuid();

        var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = userId };
        cart.Items.Add(new ShoppingCartItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            Quantity = 5,
            UnitPrice = 10m
        });
        context.ShoppingCarts.Add(cart);
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var paymentMock = new Mock<IPaymentService>();
        var cacheMock = new Mock<ICacheService>();
        var inventoryService = new InventoryService(context, cacheMock.Object);
        var logger = new CapturingLogger<CheckoutCommandHandler>();

        var handler = new CheckoutCommandHandler(
            context, currentUser.Object, paymentMock.Object, inventoryService, cacheMock.Object, logger);

        var act = async () => await handler.Handle(new CheckoutCommand(), default);

        var thrown = await act.Should().ThrowAsync<BadRequestException>();
        thrown.Which.Message.Should().Be("No hay stock suficiente para uno de los productos de tu carrito.");
        thrown.Which.Message.Should().NotMatchRegex(
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            "the client-facing message must not contain a raw GUID");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Warning && e.Message.Contains(variant.Id.ToString()),
            "the internal ProductVariantId must still be preserved for diagnostics via logging");
    }
}
