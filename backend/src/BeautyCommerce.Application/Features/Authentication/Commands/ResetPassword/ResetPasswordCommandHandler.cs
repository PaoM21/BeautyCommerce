using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.ResetPassword;

public class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Unit> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var succeeded = await _identityService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword);

        if (!succeeded)
        {
            throw new BadRequestException(
                "El enlace de restablecimiento no es válido, ya expiró, o la contraseña no cumple los requisitos mínimos.");
        }

        return Unit.Value;
    }
}
