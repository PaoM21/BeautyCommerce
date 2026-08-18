using BeautyCommerce.Application.Features.Authentication.Commands.ForgotPassword;
using FluentValidation;

namespace BeautyCommerce.Application.Features.Authentication.Validators;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Ingresa un correo electrónico válido.");
    }
}
