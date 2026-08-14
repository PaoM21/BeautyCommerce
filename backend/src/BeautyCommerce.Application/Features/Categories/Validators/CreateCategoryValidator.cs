using BeautyCommerce.Application.Features.Categories.Commands.CreateCategory;
using FluentValidation;

namespace BeautyCommerce.Application.Features.Categories.Validators;

public class CreateCategoryValidator
    : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        // Empty is a valid "no image" state — Category.ImageUrl has no
        // NotEmpty requirement anywhere today, and CreateCategoryDto
        // defaults to "". Only a NON-empty value must actually look like a
        // URL.
        RuleFor(x => x.Category.ImageUrl)
            .Must(url =>
                string.IsNullOrEmpty(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("La URL de la imagen no es válida.");
    }
}
