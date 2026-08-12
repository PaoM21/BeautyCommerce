using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Authentication.Commands.Register;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.Register;

public class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(
        IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Guid> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        return await _identityService.RegisterAsync(
            request.User.FirstName,
            request.User.LastName,
            request.User.Email,
            request.User.Password);
    }
}