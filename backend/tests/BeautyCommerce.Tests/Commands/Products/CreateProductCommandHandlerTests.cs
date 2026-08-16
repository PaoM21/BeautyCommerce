using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Products.Commands.CreateProduct;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Products;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Should_Create_Product_With_Variants_And_Images()
    {
        var context = DbContextHelper.CreateDbContext();

        var cache = new Mock<ICacheService>();
        var identifierGenerator = new ProductVariantIdentifierGenerator(context);

        var handler = new CreateProductCommandHandler(context, cache.Object, identifierGenerator);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "Description",
            BrandId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            IsFeatured = false,
            Variants = new List<CreateVariantDto>
            {
                new() { Price = 9.99m, Stock = 10 }
            },
            Images = new List<string> { "img1.jpg" }
        };

        var id = await handler.Handle(command, default);

        id.Should().NotBeEmpty();
        context.Products.Count().Should().Be(1);
        var product = context.Products.First();
        product.Variants.Count.Should().Be(1);
        product.Images.Count.Should().Be(1);

        var variant = product.Variants.First();
        variant.SKU.Should().StartWith("SKU-", "the backend must generate a SKU, the client never provides one");
        variant.Barcode.Should().StartWith("BC-", "the backend must generate a Barcode, the client never provides one");
    }

    [Fact]
    public async Task Should_Generate_Distinct_Sku_And_Barcode_For_Every_Variant_Of_The_Same_Product()
    {
        var context = DbContextHelper.CreateDbContext();

        var cache = new Mock<ICacheService>();
        var identifierGenerator = new ProductVariantIdentifierGenerator(context);

        var handler = new CreateProductCommandHandler(context, cache.Object, identifierGenerator);

        var command = new CreateProductCommand
        {
            Name = "Multi Variant Product",
            Description = "Description",
            BrandId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            IsFeatured = false,
            Variants = new List<CreateVariantDto>
            {
                new() { Price = 9.99m, Stock = 10 },
                new() { Price = 12.99m, Stock = 5 },
                new() { Price = 15.99m, Stock = 3 }
            }
        };

        await handler.Handle(command, default);

        var product = context.Products.First();
        product.Variants.Should().HaveCount(3);

        product.Variants.Select(v => v.SKU).Distinct().Should().HaveCount(
            3, "no two variants of the same product may share a SKU");
        product.Variants.Select(v => v.Barcode).Distinct().Should().HaveCount(
            3, "no two variants of the same product may share a Barcode");
        product.Variants.Should().OnlyContain(
            v => !string.IsNullOrWhiteSpace(v.SKU) && !string.IsNullOrWhiteSpace(v.Barcode));
    }
}
