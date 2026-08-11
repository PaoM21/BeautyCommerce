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
    }
}