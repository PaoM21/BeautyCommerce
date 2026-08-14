using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Products.Commands.CreateProduct;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Application.Features.Products.Queries.GetProductById;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Repositories;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;
using System.Text.Json;

namespace BeautyCommerce.Tests.Queries.Products;

public class GetProductByIdQueryHandlerTests
{
    private static async Task<(BeautyCommerce.Infrastructure.Persistence.ApplicationDbContext Context, Guid ProductId)>
        CreateProductAsync(params (string Color, string Size, decimal Price, int Stock)[] variants)
    {
        var context = DbContextHelper.CreateDbContext();
        
        var brand = new Brand { Name = "QA Brand", Description = "x" };
        var category = new Category { Name = "QA Category", Description = "x" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var cache = new Mock<ICacheService>();
        var identifierGenerator = new ProductVariantIdentifierGenerator(context);
        var handler = new CreateProductCommandHandler(context, cache.Object, identifierGenerator);

        var productId = await handler.Handle(new CreateProductCommand
        {
            Name = "QA Color Size Product",
            Description = "Description",
            BrandId = brand.Id,
            CategoryId = category.Id,
            Variants = variants
                .Select(v => new CreateVariantDto { Price = v.Price, Stock = v.Stock, Color = v.Color, Size = v.Size })
                .ToList()
        }, default);

        return (context, productId);
    }

    private static async Task<ProductDetailDto> GetProductAsync(
        BeautyCommerce.Infrastructure.Persistence.ApplicationDbContext context, Guid productId)
    {
        var repository = new ProductRepository(context);
        var handler = new GetProductByIdHandler(repository);

        return await handler.Handle(new GetProductByIdQuery { Id = productId }, default);
    }

    [Fact]
    public async Task Create_Then_Read_Persists_Color_And_Size_Alongside_Generated_Sku_And_Barcode()
    {
        var (context, productId) = await CreateProductAsync(("purple", "M", 10m, 5));

        var product = await GetProductAsync(context, productId);
        var variant = product.Variants.Should().ContainSingle().Subject;

        variant.Color.Should().Be("purple");
        variant.Size.Should().Be("M");
        variant.SKU.Should().StartWith("SKU-", "SKU generation must be unaffected by adding Color/Size");
    }

    [Fact]
    public async Task Each_Variant_Of_The_Same_Product_Keeps_Its_Own_Color_And_Size()
    {
        var (context, productId) = await CreateProductAsync(
            ("purple", "M", 10m, 5),
            ("green", "M", 12m, 3),
            ("black", "L", 15m, 7));

        var product = await GetProductAsync(context, productId);
        product.Variants.Should().HaveCount(3);

        product.Variants.Should().Contain(v => v.Color == "purple" && v.Size == "M");
        product.Variants.Should().Contain(v => v.Color == "green" && v.Size == "M");
        product.Variants.Should().Contain(v => v.Color == "black" && v.Size == "L");
        product.Variants.Select(v => v.SKU).Distinct().Should().HaveCount(3);
    }

    [Fact]
    public void ProductVariantDto_Serializes_The_Sku_Field_As_Lowercase_Sku_Not_Name()
    {
        var dto = new ProductVariantDto { Id = Guid.NewGuid(), SKU = "SKU-3201ED22", Price = 10m, Stock = 5, Color = "purple", Size = "M" };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        json.Should().Contain("\"sku\":\"SKU-3201ED22\"");
        json.Should().NotContain("\"name\"", "the wire contract must no longer expose SKU disguised as \"name\"");
    }

    [Fact]
    public async Task GetProductByIdQuery_Returns_The_Real_IsFeatured_Value()
    {
        var context = DbContextHelper.CreateDbContext();

        var brand = new Brand { Name = "QA Brand", Description = "x" };
        var category = new Category { Name = "QA Category", Description = "x" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var cache = new Mock<ICacheService>();
        var identifierGenerator = new ProductVariantIdentifierGenerator(context);
        var createHandler = new CreateProductCommandHandler(context, cache.Object, identifierGenerator);

        var productId = await createHandler.Handle(new CreateProductCommand
        {
            Name = "QA Featured Product",
            Description = "Description",
            BrandId = brand.Id,
            CategoryId = category.Id,
            IsFeatured = true,
            Variants = [new CreateVariantDto { Price = 10m, Stock = 5 }]
        }, default);

        var product = await GetProductAsync(context, productId);

        product.IsFeatured.Should().BeTrue("the product was created as featured, and the query must report that faithfully");
    }
}
