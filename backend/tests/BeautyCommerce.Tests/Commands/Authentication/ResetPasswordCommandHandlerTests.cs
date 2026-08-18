using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Authentication.Commands.ResetPassword;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Authentication;

public class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task Should_Succeed_When_Token_Is_Valid()
    {
        var identityMock = new Mock<IIdentityService>();
        identityMock
            .Setup(x => x.ResetPasswordAsync("cliente@test.com", "valid-token", "NewPass123!"))
            .ReturnsAsync(true);

        var handler = new ResetPasswordCommandHandler(identityMock.Object);

        var act = async () => await handler.Handle(
            new ResetPasswordCommand
            {
                Email = "cliente@test.com",
                Token = "valid-token",
                NewPassword = "NewPass123!",
            },
            default);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_Throw_BadRequest_When_Token_Is_Invalid()
    {
        var identityMock = new Mock<IIdentityService>();
        identityMock
            .Setup(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var handler = new ResetPasswordCommandHandler(identityMock.Object);

        var act = async () => await handler.Handle(
            new ResetPasswordCommand
            {
                Email = "cliente@test.com",
                Token = "expired-token",
                NewPassword = "NewPass123!",
            },
            default);

        await act.Should().ThrowAsync<BadRequestException>();
    }
}
