using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using FluentValidation;

namespace BeautyCommerce.Application.Features.Orders.Validators;

public class CheckoutValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutValidator()
    {
        RuleFor(x => x.RecipientName)
            .NotEmpty()
            .WithMessage("El nombre de quien recibe el pedido es obligatorio.");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("El teléfono de contacto es obligatorio.");

        RuleFor(x => x.AddressLine)
            .NotEmpty()
            .WithMessage("La dirección de envío es obligatoria.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("La ciudad de envío es obligatoria.");

        RuleFor(x => x.Department)
            .NotEmpty()
            .WithMessage("El departamento de envío es obligatorio.");
    }
}
