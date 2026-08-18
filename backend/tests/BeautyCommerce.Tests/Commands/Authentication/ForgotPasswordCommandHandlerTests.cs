using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Authentication.Commands.ForgotPassword;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BeautyCommerce.Tests.Commands.Authentication;

public class ForgotPasswordCommandHandlerTests
{
    private static IOptions<FrontendOptions> FrontendOptions() =>
        Options.Create(new FrontendOptions { BaseUrl = "http://localhost:5173" });

    [Fact]
    public async Task Should_Send_Reset_Email_When_User_Exists()
    {
        var identityMock = new Mock<IIdentityService>();
        identityMock
            .Setup(x => x.GeneratePasswordResetTokenAsync("cliente@test.com"))
            .ReturnsAsync("abc+token/123==");

        var emailMock = new Mock<IEmailService>();

        var handler = new ForgotPasswordCommandHandler(
            identityMock.Object,
            emailMock.Object,
            FrontendOptions(),
            NullLogger<ForgotPasswordCommandHandler>.Instance);

        await handler.Handle(
            new ForgotPasswordCommand { Email = "cliente@test.com" }, default);

        emailMock.Verify(
            x => x.SendAsync(
                "cliente@test.com",
                It.IsAny<string>(),
                It.Is<string>(html => html.Contains("http://localhost:5173/restablecer-password")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Not_Send_Email_When_User_Does_Not_Exist()
    {
        var identityMock = new Mock<IIdentityService>();
        identityMock
            .Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var emailMock = new Mock<IEmailService>();

        var handler = new ForgotPasswordCommandHandler(
            identityMock.Object,
            emailMock.Object,
            FrontendOptions(),
            NullLogger<ForgotPasswordCommandHandler>.Instance);

        var act = async () => await handler.Handle(
            new ForgotPasswordCommand { Email = "noexiste@test.com" }, default);

        // No debe fallar ni revelar que el usuario no existe.
        await act.Should().NotThrowAsync();

        emailMock.Verify(
            x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Not_Throw_When_Email_Sending_Fails()
    {
        var identityMock = new Mock<IIdentityService>();
        identityMock
            .Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<string>()))
            .ReturnsAsync("token");

        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Resend caído"));

        var handler = new ForgotPasswordCommandHandler(
            identityMock.Object,
            emailMock.Object,
            FrontendOptions(),
            NullLogger<ForgotPasswordCommandHandler>.Instance);

        var act = async () => await handler.Handle(
            new ForgotPasswordCommand { Email = "cliente@test.com" }, default);

        await act.Should().NotThrowAsync();
    }
}
