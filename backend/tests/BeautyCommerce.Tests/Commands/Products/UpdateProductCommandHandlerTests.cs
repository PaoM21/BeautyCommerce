using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Products.Commands.UpdateProduct;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Products;

// 6.4.5: UpdateProductCommandHandler never touches ProductVariant at all —
// it only updates Product-level fields. That's intentional: SKU and
// Barcode are technical identifiers that must stay stable once generated
// (they may end up referenced by inventory movements, orders, and future
// integrations). This test proves that guarantee explicitly, so a future
// change to UpdateProductCommandHandler can't silently start overwriting
// them.
public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Updating_A_Product_Does_Not_Change_Its_Variants_Sku_Or_Barcode()
    {
        var context = DbContextHelper.CreateDbContext();

        var brand = new Brand { Name = "Update Test Brand" };
        var category = new Category { Name = "Update Test Category" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Original Name",
            Slug = "original-name",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var identifierGenerator = new ProductVariantIdentifierGenerator(context);
        var originalSku = await identifierGenerator.GenerateSkuAsync();
        var originalBarcode = await identifierGenerator.GenerateBarcodeAsync();

        context.ProductVariants.Add(new ProductVariant
        {
            ProductId = product.Id,
            SKU = originalSku,
            Barcode = originalBarcode,
            Price = 10m,
            Stock = 5
        });
        await context.SaveChangesAsync();

        var cache = new Mock<ICacheService>();
        var handler = new UpdateProductCommandHandler(context, cache.Object);

        await handler.Handle(new UpdateProductCommand
        {
            Id = product.Id,
            Name = "Updated Name",
            Description = "Updated description",
            BrandId = brand.Id,
            CategoryId = category.Id,
            IsFeatured = true
        }, default);

        var variant = context.ProductVariants.Single(v => v.ProductId == product.Id);
        variant.SKU.Should().Be(originalSku, "SKU must remain stable once generated, even when the product is updated");
        variant.Barcode.Should().Be(originalBarcode, "Barcode must remain stable once generated, even when the product is updated");
    }
}
