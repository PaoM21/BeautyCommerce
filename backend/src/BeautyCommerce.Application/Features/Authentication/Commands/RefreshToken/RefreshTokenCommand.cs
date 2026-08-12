using BeautyCommerce.Application.Features.Authentication.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<LoginResponseDto>
{
    public RefreshTokenRequestDto Request { get; set; } = new();
}