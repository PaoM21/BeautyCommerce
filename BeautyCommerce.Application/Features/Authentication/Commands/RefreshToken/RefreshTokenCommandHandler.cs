using BeautyCommerce.Application.Features.Authentication.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, LoginResponseDto>
{
    public Task<LoginResponseDto> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException(
            "Refresh Token aún no implementado.");
    }
}