using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BeautyCommerce.Tests.Integrity;

// 6.4.2 found zero CHECK constraints anywhere in the schema and proved it by
// writing impossible values straight to PostgreSQL via raw SQL. 6.4.4
// closed that gap: migration 20260813033142_AddDomainCheckConstraints adds
// 12 CHECK constraints (Stock/Price/Cost/MinimumStock >= 0, OldPrice IS
// NULL OR OldPrice >= 0, Quantity > 0 on OrderItems/ShoppingCartItems/
// InventoryMovements, UnitPrice >= 0 on OrderItems/ShoppingCartItems,
// Total/SubTotal >= 0 on Orders). Every 6.4.2 diagnostic test below is now
// flipped into a regression test: the exact same raw-SQL writes that
// PostgreSQL used to accept must now be rejected with a check_violation
// (SQLSTATE 23514), bypassing the application layer entirely so the
// guarantee is proven at the database, not just in C#.
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
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
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
            SubTotal = 10m,
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

    private static async Task AssertCheckViolationAsync(Func<Task> writeAttempt)
    {
        var thrown = await writeAttempt.Should().ThrowAsync<PostgresException>(
            "PostgreSQL must reject this write now that the CHECK constraint exists");

        thrown.Which.SqlState.Should().Be(
            PostgresErrorCodes.CheckViolation,
            "the rejection must come from a CHECK constraint violation (23514), not some other error");
    }

    [Fact]
    public async Task Database_Rejects_Negative_Stock_On_ProductVariant()
    {
        await using var context = NewContext();

        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ProductVariants\" SET \"Stock\" = -1 WHERE \"Id\" = {_variantId}"));

        var stock = await context.ProductVariants.AsNoTracking()
            .Where(x => x.Id == _variantId).Select(x => x.Stock).FirstAsync();
        stock.Should().Be(10, "the rejected write must not have changed the stored value");
    }

    [Fact]
    public async Task Database_Rejects_Negative_Price_On_ProductVariant()
    {
        await using var context = NewContext();

        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ProductVariants\" SET \"Price\" = -10 WHERE \"Id\" = {_variantId}"));
    }

    [Fact]
    public async Task Database_Rejects_Negative_Cost_On_ProductVariant()
    {
        await using var context = NewContext();

        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ProductVariants\" SET \"Cost\" = -5 WHERE \"Id\" = {_variantId}"));
    }

    [Fact]
    public async Task Database_Rejects_Negative_OldPrice_But_Still_Allows_Null_On_ProductVariant()
    {
        await using var context = NewContext();

        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ProductVariants\" SET \"OldPrice\" = -1 WHERE \"Id\" = {_variantId}"));

        // The constraint must not turn OldPrice into a required field —
        // NULL (no previous price) is still a valid, common state.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ProductVariants\" SET \"OldPrice\" = NULL WHERE \"Id\" = {_variantId}");

        var oldPrice = await context.ProductVariants.AsNoTracking()
            .Where(x => x.Id == _variantId).Select(x => x.OldPrice).FirstAsync();
        oldPrice.Should().BeNull("OldPrice IS NULL must remain a valid state under the new constraint");
    }

    [Fact]
    public async Task Database_Rejects_Negative_MinimumStock_On_ProductVariant()
    {
        await using var context = NewContext();

        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ProductVariants\" SET \"MinimumStock\" = -1 WHERE \"Id\" = {_variantId}"));
    }

    [Fact]
    public async Task Database_Rejects_Zero_Quantity_On_OrderItem()
    {
        await using var context = NewContext();

        var id = Guid.NewGuid();
        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OrderItems"
                ("Id", "OrderId", "ProductVariantId", "Quantity", "UnitPrice", "Total", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_orderId}, {_variantId}, 0, 10, 0, now(), true, false)
            """));

        var exists = await context.OrderItems.AsNoTracking().AnyAsync(x => x.Id == id);
        exists.Should().BeFalse("the rejected insert must not have created a row");
    }

    [Fact]
    public async Task Database_Rejects_Negative_UnitPrice_On_OrderItem()
    {
        await using var context = NewContext();

        var id = Guid.NewGuid();
        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OrderItems"
                ("Id", "OrderId", "ProductVariantId", "Quantity", "UnitPrice", "Total", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_orderId}, {_variantId}, 1, -10, -10, now(), true, false)
            """));
    }

    [Fact]
    public async Task Database_Rejects_Negative_Quantity_On_ShoppingCartItem()
    {
        await using var context = NewContext();

        var id = Guid.NewGuid();
        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ShoppingCartItems"
                ("Id", "ShoppingCartId", "ProductVariantId", "Quantity", "UnitPrice", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_cartId}, {_variantId}, -3, 10, now(), true, false)
            """));

        var exists = await context.ShoppingCartItems.AsNoTracking().AnyAsync(x => x.Id == id);
        exists.Should().BeFalse("the rejected insert must not have created a row");
    }

    [Fact]
    public async Task Database_Rejects_Negative_UnitPrice_On_ShoppingCartItem()
    {
        await using var context = NewContext();

        var id = Guid.NewGuid();
        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ShoppingCartItems"
                ("Id", "ShoppingCartId", "ProductVariantId", "Quantity", "UnitPrice", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_cartId}, {_variantId}, 1, -10, now(), true, false)
            """));
    }

    [Fact]
    public async Task Database_Rejects_Negative_Quantity_On_InventoryMovement()
    {
        await using var context = NewContext();

        var id = Guid.NewGuid();
        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "InventoryMovements"
                ("Id", "ProductVariantId", "Quantity", "IsEntry", "Reason", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_variantId}, -7, true, 'QA check constraint test', now(), true, false)
            """));

        var exists = await context.InventoryMovements.AsNoTracking().AnyAsync(x => x.Id == id);
        exists.Should().BeFalse("the rejected insert must not have created a row");
    }

    [Fact]
    public async Task Database_Rejects_Negative_Total_On_Order()
    {
        await using var context = NewContext();

        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Orders\" SET \"Total\" = -1 WHERE \"Id\" = {_orderId}"));

        var total = await context.Orders.AsNoTracking()
            .Where(x => x.Id == _orderId).Select(x => x.Total).FirstAsync();
        total.Should().Be(10m, "the rejected write must not have changed the stored value");
    }

    [Fact]
    public async Task Database_Rejects_Negative_SubTotal_On_Order()
    {
        await using var context = NewContext();

        await AssertCheckViolationAsync(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Orders\" SET \"SubTotal\" = -1 WHERE \"Id\" = {_orderId}"));
    }
}
