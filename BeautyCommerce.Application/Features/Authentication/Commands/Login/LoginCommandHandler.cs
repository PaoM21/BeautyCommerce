using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Authentication.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.Login;

public class LoginCommandHandler
    : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IIdentityService _identityService;

    public LoginCommandHandler(
        IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<LoginResponseDto> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        return await _identityService.LoginAsync(
            request.Login.Email,
            request.Login.Password);
    }
}