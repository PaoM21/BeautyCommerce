using BeautyCommerce.Application.Features.Categories.Commands.UpdateCategory;
using FluentValidation;

namespace BeautyCommerce.Application.Features.Categories.Validators;

public class UpdateCategoryValidator
    : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        // Empty is a valid "no image" state — see CreateCategoryValidator
        // for why: Category.ImageUrl is never required, and a partial
        // update that doesn't touch the image would otherwise send "".
        RuleFor(x => x.Category.ImageUrl)
            .Must(url =>
                string.IsNullOrEmpty(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("La URL de la imagen no es válida.");
    }
}
