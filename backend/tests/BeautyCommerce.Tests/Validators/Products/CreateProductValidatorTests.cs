using BeautyCommerce.Application.Features.Products.Commands.CreateProduct;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Application.Features.Products.Validators;
using FluentAssertions;

namespace BeautyCommerce.Tests.Validators.Products;

public class CreateProductValidatorTests
{
    private static CreateProductCommand ValidCommand(string color = "purple", string size = "M") => new()
    {
        Name = "Test Product",
        BrandId = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        Variants =
        [
            new CreateVariantDto { Price = 10m, Stock = 5, Color = color, Size = size }
        ]
    };

    [Fact]
    public void Rejects_A_Variant_With_Blank_Color()
    {
        var validator = new CreateProductValidator();
        var command = ValidCommand(color: "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Color"));
    }

    [Fact]
    public void Rejects_A_Variant_With_Blank_Size()
    {
        var validator = new CreateProductValidator();
        var command = ValidCommand(size: "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Size"));
    }

    [Fact]
    public void Accepts_A_Variant_With_Both_Color_And_Size_Set()
    {
        var validator = new CreateProductValidator();
        var command = ValidCommand();

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_Every_Variant_In_A_Multi_Variant_Product_Independently()
    {
        var validator = new CreateProductValidator();
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            BrandId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Variants =
            [
                new CreateVariantDto { Price = 10m, Stock = 5, Color = "purple", Size = "M" },
                new CreateVariantDto { Price = 12m, Stock = 3, Color = "", Size = "L" }
            ]
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse("the second variant's blank Color must be rejected even though the first variant is valid");
    }
}
