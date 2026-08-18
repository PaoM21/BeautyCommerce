using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeautyCommerce.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly EmailOptions _options;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        HttpClient httpClient,
        IOptions<EmailOptions> options,
        ILogger<ResendEmailService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://api.resend.com/");
        }
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning(
                "Email:ApiKey no está configurado. No se envió el correo a {ToEmail} con asunto '{Subject}'. Ver docs/EMAIL_SETUP.md.",
                toEmail,
                subject);

            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        request.Content = JsonContent.Create(new
        {
            from = $"{_options.FromName} <{_options.FromAddress}>",
            to = new[] { toEmail },
            subject,
            html = htmlBody,
        });

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "Error enviando correo a {ToEmail} vía Resend. Status: {Status}. Body: {Body}",
                toEmail,
                response.StatusCode,
                body);

            throw new InvalidOperationException(
                $"No fue posible enviar el correo a {toEmail} (HTTP {(int)response.StatusCode}).");
        }
    }
}
