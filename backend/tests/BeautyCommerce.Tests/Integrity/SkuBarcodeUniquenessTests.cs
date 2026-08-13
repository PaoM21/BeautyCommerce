using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BeautyCommerce.Tests.Integrity;

// 6.4.5: the audit found 79 real variants with SKU/Barcode already unique,
// 5 with both blank ("" — NOT NULL alone doesn't stop an empty string),
// and zero code path anywhere that generated either value. Paso 4 fixed
// the 5 blank rows using the new IProductVariantIdentifierGenerator; this
// test covers Paso 5 — migration 20260813041849_AddProductVariantSkuBarcodeUniqueness,
// which adds UNIQUE(SKU), UNIQUE(Barcode), CHECK(length(trim(SKU)) > 0),
// CHECK(length(trim(Barcode)) > 0). Each test bypasses the app layer via
// raw SQL, the same way 6.4.4's CheckConstraintTests did, to prove
// PostgreSQL itself enforces the rule — not just the generator.
[Trait("Category", "Integration")]
public class SkuBarcodeUniquenessTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private Guid _brandId;
    private Guid _categoryId;
    private Guid _productId;
    private Guid _variantId;
    private string _existingSku = string.Empty;
    private string _existingBarcode = string.Empty;

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    public async Task InitializeAsync()
    {
        await using var context = NewContext();

        var brand = new Brand { Name = "QA Uniqueness Brand", Description = "QA test brand, safe to delete." };
        var category = new Category { Name = "QA Uniqueness Category", Description = "QA test category, safe to delete." };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Uniqueness Product",
            Slug = $"qa-uniqueness-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by SkuBarcodeUniquenessTests.",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var generator = new ProductVariantIdentifierGenerator(context);
        var sku = await generator.GenerateSkuAsync();
        var barcode = await generator.GenerateBarcodeAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = sku,
            Barcode = barcode,
            Price = 10m,
            Stock = 10
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        _brandId = brand.Id;
        _categoryId = category.Id;
        _productId = product.Id;
        _variantId = variant.Id;
        _existingSku = sku;
        _existingBarcode = barcode;
    }

    public async Task DisposeAsync()
    {
        await using var context = NewContext();

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

    private async Task<string> NewUniqueSkuAsync(ApplicationDbContext context) =>
        await new ProductVariantIdentifierGenerator(context).GenerateSkuAsync();

    private async Task<string> NewUniqueBarcodeAsync(ApplicationDbContext context) =>
        await new ProductVariantIdentifierGenerator(context).GenerateBarcodeAsync();

    private static async Task AssertRejectedAsync(Func<Task> writeAttempt, string expectedSqlState)
    {
        var thrown = await writeAttempt.Should().ThrowAsync<PostgresException>(
            "PostgreSQL must reject this write");

        thrown.Which.SqlState.Should().Be(expectedSqlState);
    }

    [Fact]
    public async Task Database_Rejects_Duplicate_Sku()
    {
        await using var context = NewContext();
        var barcode = await NewUniqueBarcodeAsync(context);
        var id = Guid.NewGuid();

        await AssertRejectedAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProductVariants"
                ("Id", "ProductId", "SKU", "Barcode", "Price", "Cost", "Stock", "MinimumStock", "Color", "Size", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_productId}, {_existingSku}, {barcode}, 10, 5, 1, 0, {""}, {""}, now(), true, false)
            """), PostgresErrorCodes.UniqueViolation);

        var exists = await context.ProductVariants.AsNoTracking().AnyAsync(x => x.Id == id);
        exists.Should().BeFalse("the rejected insert must not have created a row");
    }

    [Fact]
    public async Task Database_Rejects_Duplicate_Barcode()
    {
        await using var context = NewContext();
        var sku = await NewUniqueSkuAsync(context);
        var id = Guid.NewGuid();

        await AssertRejectedAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProductVariants"
                ("Id", "ProductId", "SKU", "Barcode", "Price", "Cost", "Stock", "MinimumStock", "Color", "Size", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_productId}, {sku}, {_existingBarcode}, 10, 5, 1, 0, {""}, {""}, now(), true, false)
            """), PostgresErrorCodes.UniqueViolation);

        var exists = await context.ProductVariants.AsNoTracking().AnyAsync(x => x.Id == id);
        exists.Should().BeFalse("the rejected insert must not have created a row");
    }

    [Fact]
    public async Task Database_Rejects_Blank_Sku()
    {
        await using var context = NewContext();
        var barcode = await NewUniqueBarcodeAsync(context);
        var id = Guid.NewGuid();

        await AssertRejectedAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProductVariants"
                ("Id", "ProductId", "SKU", "Barcode", "Price", "Cost", "Stock", "MinimumStock", "Color", "Size", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_productId}, {""}, {barcode}, 10, 5, 1, 0, {""}, {""}, now(), true, false)
            """), PostgresErrorCodes.CheckViolation);

        var exists = await context.ProductVariants.AsNoTracking().AnyAsync(x => x.Id == id);
        exists.Should().BeFalse("the rejected insert must not have created a row");
    }

    [Fact]
    public async Task Database_Rejects_Blank_Barcode()
    {
        await using var context = NewContext();
        var sku = await NewUniqueSkuAsync(context);
        var id = Guid.NewGuid();

        await AssertRejectedAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProductVariants"
                ("Id", "ProductId", "SKU", "Barcode", "Price", "Cost", "Stock", "MinimumStock", "Color", "Size", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_productId}, {sku}, {""}, 10, 5, 1, 0, {""}, {""}, now(), true, false)
            """), PostgresErrorCodes.CheckViolation);

        var exists = await context.ProductVariants.AsNoTracking().AnyAsync(x => x.Id == id);
        exists.Should().BeFalse("the rejected insert must not have created a row");
    }

    [Fact]
    public async Task Whitespace_Only_Sku_Is_Also_Rejected()
    {
        // Guards against exactly the escape hatch you flagged: "" is
        // blocked, but so is "   " — trim() inside the CHECK constraint
        // covers both, not just the empty-string case.
        await using var context = NewContext();
        var barcode = await NewUniqueBarcodeAsync(context);
        var id = Guid.NewGuid();

        await AssertRejectedAsync(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProductVariants"
                ("Id", "ProductId", "SKU", "Barcode", "Price", "Cost", "Stock", "MinimumStock", "Color", "Size", "CreatedAt", "IsActive", "IsDeleted")
            VALUES
                ({id}, {_productId}, {"   "}, {barcode}, 10, 5, 1, 0, {""}, {""}, now(), true, false)
            """), PostgresErrorCodes.CheckViolation);
    }
}
