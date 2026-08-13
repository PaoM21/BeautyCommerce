using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Authentication.Commands.Register;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.Register;

public class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IIdentityService _identityService;
    private readonly ICacheService _cache;

    public RegisterCommandHandler(
        IIdentityService identityService,
        ICacheService cache)
    {
        _identityService = identityService;
        _cache = cache;
    }

    public async Task<Guid> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var userId = await _identityService.RegisterAsync(
            request.User.FirstName,
            request.User.LastName,
            request.User.Email,
            request.User.Password);

        await _cache.InvalidateTagAsync("Users");

        return userId;
    }
}