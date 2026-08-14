using BeautyCommerce.Application.Features.Products.Commands.CreateProduct;
using FluentValidation;

namespace BeautyCommerce.Application.Features.Products.Validators;

public class CreateProductValidator
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.BrandId)
            .NotEmpty();

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Variants)
            .NotEmpty();
            
        RuleForEach(x => x.Variants).ChildRules(variant =>
        {
            variant.RuleFor(v => v.Color)
                .NotEmpty()
                .WithMessage("El color de la variante es obligatorio.");

            variant.RuleFor(v => v.Size)
                .NotEmpty()
                .WithMessage("La talla de la variante es obligatoria.");
        });

        RuleForEach(x => x.Images).Must(url =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("La URL de la imagen no es válida.");
    }
}