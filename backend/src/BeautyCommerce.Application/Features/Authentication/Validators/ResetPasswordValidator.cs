using BeautyCommerce.Application.Features.Authentication.Commands.ResetPassword;
using FluentValidation;

namespace BeautyCommerce.Application.Features.Authentication.Validators;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Ingresa un correo electrónico válido.");

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("El enlace de restablecimiento no es válido.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Ingresa tu nueva contraseña.");
    }
}
