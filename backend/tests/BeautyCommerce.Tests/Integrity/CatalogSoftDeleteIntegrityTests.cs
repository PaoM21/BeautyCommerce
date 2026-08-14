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
public class CatalogSoftDeleteIntegrityTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private Guid _brandId;
    private Guid _categoryId;
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

        var brand = new Brand { Name = "QA Softdelete Brand", Description = "QA test brand, safe to delete." };
        var category = new Category { Name = "QA Softdelete Category", Description = "QA test category, safe to delete." };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Softdelete Product",
            Slug = $"qa-softdelete-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by CatalogSoftDeleteIntegrityTests; removed in DisposeAsync.",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-SD-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 10,
            MinimumStock = 0
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        _brandId = brand.Id;
        _categoryId = category.Id;
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

        var orders = await context.Orders.Where(x => orderIds.Contains(x.Id)).ToListAsync();
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

        var carts = await context.ShoppingCarts.Where(x => cartIds.Contains(x.Id)).ToListAsync();
        context.ShoppingCarts.RemoveRange(carts);
        await context.SaveChangesAsync();

        var outboxMessages = await context.OutboxMessages
            .Where(x => orderIds.Select(id => id.ToString()).Any(id => x.Content.Contains(id)))
            .ToListAsync();
        context.OutboxMessages.RemoveRange(outboxMessages);
        await context.SaveChangesAsync();

        var variant = await context.ProductVariants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _variantId);
        if (variant != null) context.ProductVariants.Remove(variant);

        var product = await context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _productId);
        if (product != null) context.Products.Remove(product);

        await context.SaveChangesAsync();

        var category = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _categoryId);
        if (category != null) context.Categories.Remove(category);

        var brand = await context.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _brandId);
        if (brand != null) context.Brands.Remove(brand);

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

    private static async Task<Guid?> RunCheckoutAsync(ApplicationDbContext context, Guid userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var payment = new Mock<IPaymentService>();
        payment
            .Setup(p => p.CreatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(PaymentResult.Succeeded($"tx-{userId:N}"));

        var cache = new Mock<ICacheService>();
        var inventoryService = new InventoryService(context, cache.Object);

        var handler = new CheckoutCommandHandler(
            context, currentUser.Object, payment.Object, inventoryService, cache.Object, NullLogger<CheckoutCommandHandler>.Instance);

        try
        {
            return await handler.Handle(new CheckoutCommand(), default);
        }
        catch (BeautyCommerce.Application.Common.Exceptions.BadRequestException)
        {
            return null;
        }
    }

    [Fact]
    public async Task Checkout_Behavior_When_The_Products_Own_Product_Is_Soft_Deleted()
    {
        await using (var setupContext = NewContext())
        {
            var product = await setupContext.Products.FirstAsync(x => x.Id == _productId);
            product.IsActive = false;
            product.IsDeleted = true;
            product.DeletedAt = DateTime.UtcNow;
            await setupContext.SaveChangesAsync();
        }

        var userId = Guid.NewGuid();
        await using (var seedContext = NewContext())
            await SeedCartAsync(seedContext, userId, _variantId, quantity: 3);

        await using var checkoutContext = NewContext();
        var orderId = await RunCheckoutAsync(checkoutContext, userId);

        orderId.Should().BeNull(
            "checkout must reject a cart item whose parent Product has been soft-deleted, even though the " +
            "variant's own IsDeleted is still false");

        await using var verify = NewContext();

        var stock = await verify.ProductVariants.AsNoTracking()
            .Where(x => x.Id == _variantId).Select(x => x.Stock).FirstAsync();
        stock.Should().Be(10, "a rejected checkout must not reserve/consume any stock");

        var movements = await verify.InventoryMovements.AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId).ToListAsync();
        movements.Should().BeEmpty("a rejected checkout must not create any inventory movement");

        var orderItems = await verify.OrderItems.AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId).ToListAsync();
        orderItems.Should().BeEmpty("a rejected checkout must not create any order item");

        var orders = await verify.Orders.AsNoTracking()
            .Where(x => x.UserId == userId).ToListAsync();
        orders.Should().BeEmpty("a rejected checkout must not create any order");

        var cartItems = await verify.ShoppingCartItems.AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId).ToListAsync();
        cartItems.Should().HaveCount(1, "the cart item must remain untouched after a rejected checkout");
        cartItems[0].Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Checkout_Behavior_When_The_Products_Category_Is_Soft_Deleted()
    {
        await using (var setupContext = NewContext())
        {
            var category = await setupContext.Categories.FirstAsync(x => x.Id == _categoryId);
            category.IsActive = false;
            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            await setupContext.SaveChangesAsync();
        }

        var userId = Guid.NewGuid();
        await using (var seedContext = NewContext())
            await SeedCartAsync(seedContext, userId, _variantId, quantity: 2);

        await using var checkoutContext = NewContext();
        var orderId = await RunCheckoutAsync(checkoutContext, userId);

        orderId.Should().BeNull(
            "checkout must reject a cart item whose Product's Category has been soft-deleted, even though " +
            "neither the variant nor the product itself is marked deleted");

        await using var verify = NewContext();

        var stock = await verify.ProductVariants.AsNoTracking()
            .Where(x => x.Id == _variantId).Select(x => x.Stock).FirstAsync();
        stock.Should().Be(10, "a rejected checkout must not reserve/consume any stock");

        var movements = await verify.InventoryMovements.AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId).ToListAsync();
        movements.Should().BeEmpty("a rejected checkout must not create any inventory movement");

        var orderItems = await verify.OrderItems.AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId).ToListAsync();
        orderItems.Should().BeEmpty("a rejected checkout must not create any order item");

        var orders = await verify.Orders.AsNoTracking()
            .Where(x => x.UserId == userId).ToListAsync();
        orders.Should().BeEmpty("a rejected checkout must not create any order");

        var cartItems = await verify.ShoppingCartItems.AsNoTracking()
            .Where(x => x.ProductVariantId == _variantId).ToListAsync();
        cartItems.Should().HaveCount(1, "the cart item must remain untouched after a rejected checkout");
        cartItems[0].Quantity.Should().Be(2);
    }
}
