using BeautyCommerce.Application.Features.Authentication.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.Login;

public class LoginCommand : IRequest<LoginResponseDto>
{
    public LoginRequestDto Login { get; set; } = new();
}