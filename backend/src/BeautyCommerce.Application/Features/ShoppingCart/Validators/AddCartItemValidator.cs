using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using FluentValidation;

namespace BeautyCommerce.Application.Features.ShoppingCart.Validators;

public class AddCartItemValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemValidator()
    {
        RuleFor(x => x.Item.Quantity)
            .GreaterThan(0)
            .WithMessage("La cantidad debe ser mayor que cero.");
    }
}
