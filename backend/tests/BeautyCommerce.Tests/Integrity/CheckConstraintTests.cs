using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Tests.Integrity;

// 6.4.2 finding #4: 6.4.1 found zero CHECK constraints anywhere in the
// schema. This test proves it directly — writing impossible values
// (negative stock, negative price, zero/negative quantities) straight to
// PostgreSQL via raw SQL, bypassing every C# validation the application
// has (InventoryService's atomic UPDATE guard, FluentValidation, etc.
// none of that runs here on purpose). If PostgreSQL accepts these writes,
// there is no safety net below the application layer — a future bug, a
// direct SQL script, or a different service touching this data could
// silently corrupt it.
[Trait("Category", "Integration")]
public class CheckConstraintTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private Guid _brandId;
    private Guid _categoryId;
    private Guid _productId;
    private Guid _variantId;
    private Guid _orderId;
    private Guid _cartId;

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    public async Task InitializeAsync()
    {
        await using var context = NewContext();

        var brand = new Brand { Name = "QA Constraint Brand", Description = "QA test brand, safe to delete." };
        var category = new Category { Name = "QA Constraint Category", Description = "QA test category, safe to delete." };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Constraint Product",
            Slug = $"qa-constraint-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by CheckConstraintTests.",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-CHK-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Cost = 5m,
            Stock = 10,
            MinimumStock = 0
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        var order = new Order
        {
            UserId = Guid.NewGuid(),
            OrderNumber = $"ORD-QA-CHECK-{Guid.NewGuid():N}"[..30],
            Status = BeautyCommerce.Domain.Enums.OrderStatus.Paid,
            Total = 10m,
            OrderDate = DateTime.UtcNow
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var cart = new ShoppingCart { UserId = Guid.NewGuid() };
        context.ShoppingCarts.Add(cart);
        await context.SaveChangesAsync();

        _brandId = brand.Id;
        _categoryId = category.Id;
        _productId = product.Id;
        _variantId = variant.Id;
        _orderId = order.Id;
        _cartId = cart.Id;
    }

    public async Task DisposeAsync()
    {
        await using var context = NewContext();

        var movements = await context.InventoryMovements.IgnoreQueryFilters()
            .Where(x => x.ProductVariantId == _variantId).ToListAsync();
        context.InventoryMovements.RemoveRange(movements);
        await context.SaveChangesAsync();

        var orderItems = await context.OrderItems.IgnoreQueryFilters()
            .Where(x => x.OrderId == _orderId).ToListAsync();
        context.OrderItems.RemoveRange(orderItems);
        await context.SaveChangesAsync();

        var cartItems = await context.ShoppingCartItems.IgnoreQueryFilters()
            .Where(x => x.ShoppingCartId == _cartId).ToListAsync();
        context.ShoppingCartItems.RemoveRange(cartItems);
        await context.SaveChangesAsync();

        var order = await context.Orders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _orderId);
        if (order != null) context.Orders.Remove(order);

        var cart = await context.ShoppingCarts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _cartId);
        if (cart != null) context.ShoppingCarts.Remove(cart);

        var variant = await context.ProductVariants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _variantId);
        if (variant != null) context.ProductVariants.Remove(variant);

        await context.SaveChangesAsync();

        var product = await context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _productId);
        if (product != null) context.Products.Remove(product);

        await context.SaveChangesAsync();

        var category = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _categoryId);
        if (category != null) context.Categories.Remove(category);

        var brand = await context.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _brandId);
        if (brand != null) context.Brands.Remove(brand);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Database_Accepts_Negative_Stock_On_ProductVariant()
    {
        await using var context = NewContext();

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ProductVariants\" SET \"Stock\" = -1 WHERE \"Id\" = {_variantId}");

        var stock = await context.ProductVariants.AsNoTracking()
            .Where(x => x.Id == _variantId).Select(x => x.Stock).FirstAsync();

        stock.Should().Be(-1, "no CHECK constraint exists on ProductVariants.Stock — PostgreSQL accepted a raw UPDATE to a negative value");
    }

    [Fact]
    public async Task Database_Accepts_Negative_Price_And_Cost_On_ProductVariant()
    {
        await using var context = NewContext();

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ProductVariants\" SET \"Price\" = -10, \"Cost\" = -5 WHERE \"Id\" = {_variantId}");

        var variant = await context.ProductVariants.AsNoTracking()
            .FirstAsync(x => x.Id == _variantId);

        variant.Price.Should().Be(-10m, "no CHECK constraint exists on ProductVariants.Price");
        variant.Cost.Should().Be(-5m, "no CHECK constraint exists on ProductVariants.Cost");
    }

    [Fact]
    public async Task Database_Accepts_Zero_Quantity_On_OrderItem()
    {
        await using var context = NewContext();

        var id = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OrderItems"
                ("Id", "OrderId", "ProductVariantId", "Quantity", "UnitPrice", "Total", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_orderId}, {_variantId}, 0, 10, 0, now(), true, false)
            """);

        var exists = await context.OrderItems.AsNoTracking().AnyAsync(x => x.Id == id);
        exists.Should().BeTrue("no CHECK constraint exists on OrderItems.Quantity — a zero-quantity line item was accepted");
    }

    [Fact]
    public async Task Database_Accepts_Negative_Quantity_On_ShoppingCartItem()
    {
        await using var context = NewContext();

        var id = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ShoppingCartItems"
                ("Id", "ShoppingCartId", "ProductVariantId", "Quantity", "UnitPrice", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_cartId}, {_variantId}, -3, 10, now(), true, false)
            """);

        var quantity = await context.ShoppingCartItems.AsNoTracking()
            .Where(x => x.Id == id).Select(x => x.Quantity).FirstAsync();

        quantity.Should().Be(-3, "no CHECK constraint exists on ShoppingCartItems.Quantity — a -3 quantity cart line was accepted");
    }

    [Fact]
    public async Task Database_Accepts_Negative_Quantity_On_InventoryMovement()
    {
        await using var context = NewContext();

        var id = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "InventoryMovements"
                ("Id", "ProductVariantId", "Quantity", "IsEntry", "Reason", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_variantId}, -7, true, 'QA check constraint test', now(), true, false)
            """);

        var quantity = await context.InventoryMovements.AsNoTracking()
            .Where(x => x.Id == id).Select(x => x.Quantity).FirstAsync();

        quantity.Should().Be(-7, "no CHECK constraint exists on InventoryMovements.Quantity — a negative-quantity movement was accepted, " +
            "even though it would make an 'entry' subtract stock instead of adding it");
    }
}
