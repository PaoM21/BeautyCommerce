using BeautyCommerce.Application.Features.ShoppingCart.Commands.UpdateItem;
using FluentValidation;

namespace BeautyCommerce.Application.Features.ShoppingCart.Validators;

public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x.Item.Quantity)
            .GreaterThan(0)
            .WithMessage("La cantidad debe ser mayor que cero.");
    }
}
