using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Tests.Integrity;

// 6.4.2 finding #2: ProductVariantConfiguration and InventoryMovementConfiguration
// both use OnDelete(DeleteBehavior.Cascade). This test proves, against real
// PostgreSQL, what a real DELETE of a Product actually does to its
// variants and their inventory movement history. Today the application
// never issues this DELETE (DeleteProductCommandHandler soft-deletes), so
// this is deliberately bypassing the app layer and calling
// context.Products.Remove(...) directly — the point is to prove what the
// DATABASE allows, independent of what the current application code
// happens to do.
[Trait("Category", "Integration")]
public class CascadeDeleteTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    [Fact]
    public async Task Hard_Deleting_A_Product_Cascades_Through_Variants_Into_Inventory_History()
    {
        Guid brandId, categoryId, productId, variantId, movementId;

        await using (var context = NewContext())
        {
            var brand = new Brand { Name = "QA Cascade Brand", Description = "QA test brand, safe to delete." };
            var category = new Category { Name = "QA Cascade Category", Description = "QA test category, safe to delete." };
            context.Brands.Add(brand);
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var product = new Product
            {
                Name = "QA Cascade Product",
                Slug = $"qa-cascade-{Guid.NewGuid():N}",
                ShortDescription = "QA test product, safe to delete.",
                Description = "Created by CascadeDeleteTests.",
                BrandId = brand.Id,
                CategoryId = category.Id
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = product.Id,
                SKU = $"QA-CASC-{Guid.NewGuid():N}"[..20],
                Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
                Price = 10m,
                Stock = 5,
                MinimumStock = 0
            };
            context.ProductVariants.Add(variant);
            await context.SaveChangesAsync();

            var movement = new InventoryMovement
            {
                ProductVariantId = variant.Id,
                Quantity = 5,
                IsEntry = true,
                Reason = "QA cascade test — initial stock, this is historical inventory data"
            };
            context.InventoryMovements.Add(movement);
            await context.SaveChangesAsync();

            brandId = brand.Id;
            categoryId = category.Id;
            productId = product.Id;
            variantId = variant.Id;
            movementId = movement.Id;
        }

        try
        {
            // The real DELETE — bypassing the application's soft-delete
            // convention entirely, to observe what PostgreSQL's own FK
            // cascade rules do.
            await using (var context = NewContext())
            {
                var product = await context.Products.FirstAsync(x => x.Id == productId);
                context.Products.Remove(product);
                await context.SaveChangesAsync();
            }

            await using var verify = NewContext();

            var productStillExists = await verify.Products.IgnoreQueryFilters().AnyAsync(x => x.Id == productId);
            var variantStillExists = await verify.ProductVariants.IgnoreQueryFilters().AnyAsync(x => x.Id == variantId);
            var movementStillExists = await verify.InventoryMovements.IgnoreQueryFilters().AnyAsync(x => x.Id == movementId);

            productStillExists.Should().BeFalse("the DELETE itself removes the product, as expected");

            variantStillExists.Should().BeFalse(
                "current schema: ProductVariants.ProductId is ON DELETE CASCADE, so deleting the Product " +
                "silently deletes its variants too");

            movementStillExists.Should().BeFalse(
                "current schema: InventoryMovements.ProductVariantId is ALSO ON DELETE CASCADE, so the cascade " +
                "reaches all the way through to inventory movement history — a real Product DELETE destroys " +
                "audit trail data, not just catalog data");
        }
        finally
        {
            // Cleanup: the cascade (if it happened) already removed
            // Product/Variant/Movement. Brand/Category are independent
            // (Products -> Brand/Category is RESTRICT, not something that
            // would block deleting the brand/category themselves once no
            // product references them).
            await using var cleanup = NewContext();

            var leftoverMovement = await cleanup.InventoryMovements.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == movementId);
            if (leftoverMovement != null) cleanup.InventoryMovements.Remove(leftoverMovement);

            var leftoverVariant = await cleanup.ProductVariants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == variantId);
            if (leftoverVariant != null) cleanup.ProductVariants.Remove(leftoverVariant);

            var leftoverProduct = await cleanup.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == productId);
            if (leftoverProduct != null) cleanup.Products.Remove(leftoverProduct);

            await cleanup.SaveChangesAsync();

            var category = await cleanup.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == categoryId);
            if (category != null) cleanup.Categories.Remove(category);

            var brand = await cleanup.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == brandId);
            if (brand != null) cleanup.Brands.Remove(brand);

            await cleanup.SaveChangesAsync();
        }
    }
}
