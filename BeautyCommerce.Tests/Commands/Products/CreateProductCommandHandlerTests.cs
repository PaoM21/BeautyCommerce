using BeautyCommerce.Application.Features.Products.Commands.CreateProduct;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;

namespace BeautyCommerce.Tests.Commands.Products;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Should_Create_Product_With_Variants_And_Images()
    {
        var context = DbContextHelper.CreateDbContext();

        var handler = new CreateProductCommandHandler(context);

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
    }
}
