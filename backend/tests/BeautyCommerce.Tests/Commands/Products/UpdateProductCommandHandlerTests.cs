using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Products.Commands.UpdateProduct;
using BeautyCommerce.Application.Features.Products.Queries.GetProductById;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Repositories;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BeautyCommerce.Tests.Commands.Products;

public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Updating_A_Product_Persists_Its_Own_Fields_And_Does_Not_Change_Its_Variants_Sku_Or_Barcode()
    {
        var context = DbContextHelper.CreateDbContext();

        var brand = new Brand { Name = "Update Test Brand" };
        var category = new Category { Name = "Update Test Category" };
        var newBrand = new Brand { Name = "New Update Test Brand" };
        var newCategory = new Category { Name = "New Update Test Category" };
        context.Brands.AddRange(brand, newBrand);
        context.Categories.AddRange(category, newCategory);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Original Name",
            Slug = "original-name",
            BrandId = brand.Id,
            CategoryId = category.Id,
            IsFeatured = false
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
            BrandId = newBrand.Id,
            CategoryId = newCategory.Id,
            IsFeatured = true
        }, default);
        
        var updated = context.Products.AsNoTracking().Single(p => p.Id == product.Id);
        updated.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Updated description");
        updated.BrandId.Should().Be(newBrand.Id);
        updated.CategoryId.Should().Be(newCategory.Id);
        updated.IsFeatured.Should().BeTrue();

        var variant = context.ProductVariants.Single(v => v.ProductId == product.Id);
        variant.SKU.Should().Be(originalSku, "SKU must remain stable once generated, even when the product is updated");
        variant.Barcode.Should().Be(originalBarcode, "Barcode must remain stable once generated, even when the product is updated");
    }

    [Fact]
    public async Task Editing_Only_Name_Preserves_IsFeatured_When_The_Caller_Correctly_Preloads_It()
    {
        var context = DbContextHelper.CreateDbContext();

        var brand = new Brand { Name = "Update Test Brand" };
        var category = new Category { Name = "Update Test Category" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Featured Product",
            Slug = "featured-product",
            BrandId = brand.Id,
            CategoryId = category.Id,
            IsFeatured = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var getHandler = new GetProductByIdHandler(new ProductRepository(context));
        var loaded = await getHandler.Handle(new GetProductByIdQuery { Id = product.Id }, default);

        loaded.IsFeatured.Should().BeTrue("GetProductByIdQuery must now return the real IsFeatured value");

        var cache = new Mock<ICacheService>();
        var updateHandler = new UpdateProductCommandHandler(context, cache.Object);

        await updateHandler.Handle(new UpdateProductCommand
        {
            Id = product.Id,
            Name = "Featured Product (edited)",
            Description = loaded.Description,
            BrandId = loaded.BrandId,
            CategoryId = loaded.CategoryId,
            IsFeatured = loaded.IsFeatured
        }, default);

        var reloaded = context.Products.AsNoTracking().Single(p => p.Id == product.Id);
        reloaded.Name.Should().Be("Featured Product (edited)");
        reloaded.IsFeatured.Should().BeTrue(
            "editing only the name must not un-feature the product now that the real value round-trips correctly");
    }
}
