using BeautyCommerce.Application.Common.Emails;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Authentication.Commands.Register;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BeautyCommerce.Application.Features.Authentication.Commands.Register;

public class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IIdentityService _identityService;
    private readonly ICacheService _cache;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IIdentityService identityService,
        ICacheService cache,
        IEmailService emailService,
        ILogger<RegisterCommandHandler> logger)
    {
        _identityService = identityService;
        _cache = cache;
        _emailService = emailService;
        _logger = logger;
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

        var (subject, html) = EmailTemplates.AccountCreated(request.User.FirstName);

        try
        {
            await _emailService.SendAsync(
                request.User.Email, subject, html, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Error enviando correo de bienvenida a {Email}", request.User.Email);
        }

        return userId;
    }
}