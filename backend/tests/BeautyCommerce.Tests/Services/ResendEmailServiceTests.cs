using System.Net;
using System.Text.Json;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BeautyCommerce.Tests.Services;

public class ResendEmailServiceTests
{
    private static (ResendEmailService Service, FakeHttpMessageHandler Handler) CreateService(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string apiKey = "test-key")
    {
        var handler = new FakeHttpMessageHandler(statusCode);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new EmailOptions
        {
            ApiKey = apiKey,
            FromAddress = "onboarding@resend.dev",
            FromName = "HALDY&CO ECOMMERCE",
        });

        var service = new ResendEmailService(
            httpClient, options, NullLogger<ResendEmailService>.Instance);

        return (service, handler);
    }

    [Fact]
    public async Task Should_Send_Request_With_Correct_Headers_And_Body()
    {
        var (service, handler) = CreateService();

        await service.SendAsync("cliente@test.com", "Asunto de prueba", "<p>Hola</p>");

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://api.resend.com/emails");
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("test-key");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;

        root.GetProperty("from").GetString().Should().Be("HALDY&CO ECOMMERCE <onboarding@resend.dev>");
        root.GetProperty("subject").GetString().Should().Be("Asunto de prueba");
        root.GetProperty("to")[0].GetString().Should().Be("cliente@test.com");
    }

    [Fact]
    public async Task Should_Throw_When_Resend_Returns_Error_Status()
    {
        var (service, _) = CreateService(HttpStatusCode.BadRequest);

        var act = async () =>
            await service.SendAsync("cliente@test.com", "Asunto", "<p>Hola</p>");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_Not_Call_Http_When_ApiKey_Is_Missing()
    {
        var (service, handler) = CreateService(apiKey: "");

        await service.SendAsync("cliente@test.com", "Asunto", "<p>Hola</p>");

        handler.LastRequest.Should().BeNull();
    }
}
