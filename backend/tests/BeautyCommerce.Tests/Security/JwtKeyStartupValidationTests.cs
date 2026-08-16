using BeautyCommerce.Application.Common.Settings;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text;

namespace BeautyCommerce.Tests.Security;

public class JwtKeyStartupValidationTests
{
    private static IHost BuildHost(Dictionary<string, string?> configValues)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(configValues);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions<JwtSettings>()
                    .Bind(context.Configuration.GetSection("Jwt"))
                    .Validate(
                        settings => !string.IsNullOrWhiteSpace(settings.Key) &&
                                    Encoding.UTF8.GetByteCount(settings.Key) >= 16,
                        "Jwt:Key debe estar presente y tener al menos 16 bytes (128 bits), el mínimo requerido por el algoritmo HS256.")
                    .ValidateOnStart();
            })
            .Build();
    }

    [Fact]
    public async Task Missing_Jwt_Key_Rejects_Startup()
    {
        using var host = BuildHost(new Dictionary<string, string?>());

        var act = async () => await host.StartAsync();

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public async Task Empty_Jwt_Key_Rejects_Startup()
    {
        using var host = BuildHost(new Dictionary<string, string?> { ["Jwt:Key"] = "" });

        var act = async () => await host.StartAsync();

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public async Task Jwt_Key_Shorter_Than_16_Bytes_Rejects_Startup()
    {
        using var host = BuildHost(new Dictionary<string, string?> { ["Jwt:Key"] = "short" });

        var act = async () => await host.StartAsync();

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public async Task Jwt_Key_Of_16_Bytes_Or_More_Allows_Startup()
    {
        using var host = BuildHost(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "exactly-16-bytes"
        });

        var act = async () => await host.StartAsync();

        await act.Should().NotThrowAsync();

        await host.StopAsync();
    }
}
