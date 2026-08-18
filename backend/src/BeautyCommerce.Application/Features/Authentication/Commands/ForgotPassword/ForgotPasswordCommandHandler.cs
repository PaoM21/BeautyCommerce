using System.Net;
using BeautyCommerce.Application.Common.Emails;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeautyCommerce.Application.Features.Authentication.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IEmailService emailService,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _identityService = identityService;
        _emailService = emailService;
        _frontendOptions = frontendOptions.Value;
        _logger = logger;
    }

    public async Task<Unit> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var token = await _identityService.GeneratePasswordResetTokenAsync(request.Email);

        // Si el usuario no existe, no hacemos nada más — la respuesta al
        // cliente es siempre la misma (ver AuthController) para no revelar
        // qué correos están registrados.
        if (token == null)
        {
            return Unit.Value;
        }

        var resetUrl =
            $"{_frontendOptions.BaseUrl.TrimEnd('/')}/restablecer-password" +
            $"?email={WebUtility.UrlEncode(request.Email)}" +
            $"&token={WebUtility.UrlEncode(token)}";

        var (subject, html) = EmailTemplates.PasswordReset(resetUrl);

        try
        {
            await _emailService.SendAsync(request.Email, subject, html, cancellationToken);
        }
        catch (Exception ex)
        {
            // No propagamos el error al cliente: si Resend falla, el
            // usuario no debe saber si su correo existe o no ni por qué
            // falló. Queda registrado para diagnóstico.
            _logger.LogError(
                ex, "Error enviando correo de reseteo de contraseña a {Email}", request.Email);
        }

        return Unit.Value;
    }
}
