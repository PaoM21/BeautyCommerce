using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Authentication.Commands.Register;
using BeautyCommerce.Application.Features.Authentication.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Authentication;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Should_Send_Welcome_Email_After_Registering()
    {
        var userId = Guid.NewGuid();

        var identityMock = new Mock<IIdentityService>();
        identityMock
            .Setup(x => x.RegisterAsync(
                "Ana", "Pérez", "ana@test.com", "Password123!"))
            .ReturnsAsync(userId);

        var cacheMock = new Mock<ICacheService>();
        var emailMock = new Mock<IEmailService>();

        var handler = new RegisterCommandHandler(
            identityMock.Object,
            cacheMock.Object,
            emailMock.Object,
            NullLogger<RegisterCommandHandler>.Instance);

        var result = await handler.Handle(
            new RegisterCommand
            {
                User = new RegisterRequestDto
                {
                    FirstName = "Ana",
                    LastName = "Pérez",
                    Email = "ana@test.com",
                    Password = "Password123!",
                    ConfirmPassword = "Password123!",
                }
            },
            default);

        result.Should().Be(userId);

        emailMock.Verify(
            x => x.SendAsync(
                "ana@test.com",
                It.IsAny<string>(),
                It.Is<string>(html => html.Contains("Ana")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        cacheMock.Verify(c => c.InvalidateTagAsync("Users"), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Throw_When_Welcome_Email_Fails()
    {
        var identityMock = new Mock<IIdentityService>();
        identityMock
            .Setup(x => x.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Guid.NewGuid());

        var cacheMock = new Mock<ICacheService>();

        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Resend caído"));

        var handler = new RegisterCommandHandler(
            identityMock.Object,
            cacheMock.Object,
            emailMock.Object,
            NullLogger<RegisterCommandHandler>.Instance);

        var act = async () => await handler.Handle(
            new RegisterCommand
            {
                User = new RegisterRequestDto
                {
                    FirstName = "Ana",
                    LastName = "Pérez",
                    Email = "ana@test.com",
                    Password = "Password123!",
                    ConfirmPassword = "Password123!",
                }
            },
            default);

        await act.Should().NotThrowAsync();
    }
}
