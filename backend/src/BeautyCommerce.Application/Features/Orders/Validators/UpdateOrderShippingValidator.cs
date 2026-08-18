using BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderShipping;
using FluentValidation;

namespace BeautyCommerce.Application.Features.Orders.Validators;

public class UpdateOrderShippingValidator : AbstractValidator<UpdateOrderShippingCommand>
{
    public UpdateOrderShippingValidator()
    {
        RuleFor(x => x.Carrier)
            .NotEmpty()
            .WithMessage("La transportadora es obligatoria.");

        RuleFor(x => x.TrackingNumber)
            .NotEmpty()
            .WithMessage("El número de guía es obligatorio.");
    }
}
