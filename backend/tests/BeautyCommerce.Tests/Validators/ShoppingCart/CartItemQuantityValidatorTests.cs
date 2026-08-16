using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.UpdateItem;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Application.Features.ShoppingCart.Validators;
using FluentAssertions;

namespace BeautyCommerce.Tests.Validators.ShoppingCart;

// 8.3 (fresh recon) — regression: neither AddCartItem nor UpdateCartItem had
// a FluentValidation validator for Quantity, so a client could send
// Quantity <= 0 straight through to the database, where it only surfaced as
// an unhandled 500 when it hit the real CK_ShoppingCartItems_Quantity_Positive
// check constraint. These validators close the gap at the application layer.
public class CartItemQuantityValidatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddCartItemValidator_Rejects_NonPositive_Quantity(int quantity)
    {
        var validator = new AddCartItemValidator();
        var command = new AddCartItemCommand
        {
            Item = new AddCartItemDto { ProductVariantId = Guid.NewGuid(), Quantity = quantity }
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "La cantidad debe ser mayor que cero.");
    }

    [Fact]
    public void AddCartItemValidator_Accepts_Positive_Quantity()
    {
        var validator = new AddCartItemValidator();
        var command = new AddCartItemCommand
        {
            Item = new AddCartItemDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateCartItemValidator_Rejects_NonPositive_Quantity(int quantity)
    {
        var validator = new UpdateCartItemValidator();
        var command = new UpdateCartItemCommand
        {
            Item = new UpdateCartItemDto { ProductVariantId = Guid.NewGuid(), Quantity = quantity }
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "La cantidad debe ser mayor que cero.");
    }

    [Fact]
    public void UpdateCartItemValidator_Accepts_Positive_Quantity()
    {
        var validator = new UpdateCartItemValidator();
        var command = new UpdateCartItemCommand
        {
            Item = new UpdateCartItemDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }
}
