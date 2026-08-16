using BeautyCommerce.Application.Common.Settings;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeautyCommerce.Tests.Security;

public class CorsConfigurationTests
{
    private static WebApplication BuildApp(Dictionary<string, string?> configValues)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(configValues);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        builder.Services.AddOptions<CorsSettings>()
            .Bind(builder.Configuration.GetSection("Cors"))
            .Validate(
                settings => settings.AllowedOrigins is { Length: > 0 } &&
                            settings.AllowedOrigins.All(origin => !string.IsNullOrWhiteSpace(origin)),
                "Cors:AllowedOrigins debe contener al menos un origin no vacío.")
            .ValidateOnStart();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DefaultCorsPolicy", policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod()
                    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>());
            });
        });

        var app = builder.Build();
        app.UseCors("DefaultCorsPolicy");
        app.MapGet("/probe", () => Results.Ok());

        return app;
    }

    private static async Task<(WebApplication App, HttpClient Client)> StartAsync(Dictionary<string, string?> configValues)
    {
        var app = BuildApp(configValues);
        await app.StartAsync();

        var address = app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        return (app, new HttpClient { BaseAddress = new Uri(address) });
    }

    private static readonly Dictionary<string, string?> DevConfig = new()
    {
        ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
    };

    [Fact]
    public async Task Allowed_Origin_Receives_Access_Control_Allow_Origin_Header()
    {
        var (app, client) = await StartAsync(DevConfig);
        await using var _ = app;

        var request = new HttpRequestMessage(HttpMethod.Get, "/probe");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values)
            .Should().BeTrue();
        values!.Single().Should().Be("http://localhost:5173");
    }

    [Fact]
    public async Task Disallowed_Origin_Receives_No_Access_Control_Allow_Origin_Header()
    {
        var (app, client) = await StartAsync(DevConfig);
        await using var _ = app;

        var request = new HttpRequestMessage(HttpMethod.Get, "/probe");
        request.Headers.Add("Origin", "http://evil.com");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task Preflight_From_Allowed_Origin_Is_Approved()
    {
        var (app, client) = await StartAsync(DevConfig);
        await using var _ = app;

        var request = new HttpRequestMessage(HttpMethod.Options, "/probe");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins)
            .Should().BeTrue();
        origins!.Single().Should().Be("http://localhost:5173");
        response.Headers.Contains("Access-Control-Allow-Methods").Should().BeTrue();
    }

    [Fact]
    public async Task Preflight_From_Disallowed_Origin_Is_Rejected()
    {
        var (app, client) = await StartAsync(DevConfig);
        await using var _ = app;

        var request = new HttpRequestMessage(HttpMethod.Options, "/probe");
        request.Headers.Add("Origin", "http://evil.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        response.Headers.Contains("Access-Control-Allow-Methods").Should().BeFalse();
    }

    [Fact]
    public async Task Missing_Allowed_Origins_Rejects_Startup()
    {
        var app = BuildApp(new Dictionary<string, string?>());

        var act = async () => await app.StartAsync();

        await act.Should().ThrowAsync<OptionsValidationException>();
    }
}
