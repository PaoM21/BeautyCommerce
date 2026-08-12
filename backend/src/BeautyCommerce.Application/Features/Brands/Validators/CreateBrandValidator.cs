using BeautyCommerce.Application.Features.Brands.Commands.CreateBrand;
using FluentValidation;

namespace BeautyCommerce.Application.Features.Brands.Validators;

public class CreateBrandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandValidator()
    {
        RuleFor(x => x.Brand.Name)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.")
            .MaximumLength(100)
            .WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Brand.Description)
            .NotEmpty()
            .WithMessage("La descripción es obligatoria.");

        RuleFor(x => x.Brand.Description)
            .MaximumLength(500)
            .WithMessage("La descripción no puede superar los 500 caracteres.");

        RuleFor(x => x.Brand.LogoUrl)
            .MaximumLength(500)
            .WithMessage("La URL del logo es demasiado larga.");
    }
}