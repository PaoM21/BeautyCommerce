using BeautyCommerce.Application.Features.Authentication.Commands.Login;
using BeautyCommerce.Application.Features.Authentication.Commands.Register;
using BeautyCommerce.Application.Features.Authentication.DTOs;
using Moq;
using FluentAssertions;

namespace BeautyCommerce.Tests.Commands.Auth;

public class LoginRegisterHandlerTests
{
    [Fact]
    public async Task LoginHandler_Invokes_IdentityService()
    {
        var mock = new Mock<BeautyCommerce.Application.Common.Interfaces.IIdentityService>();
        var expected = new LoginResponseDto { Token = "t" };
        mock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(expected);

        var handler = new LoginCommandHandler(mock.Object);

        var command = new LoginCommand { Login = new LoginRequestDto { Email = "a@b.com", Password = "p" } };

        var result = await handler.Handle(command, default);

        result.Should().BeEquivalentTo(expected);
        mock.Verify(x => x.LoginAsync(command.Login.Email, command.Login.Password), Times.Once);
    }

    [Fact]
    public async Task RegisterHandler_Invokes_IdentityService()
    {
        var mock = new Mock<BeautyCommerce.Application.Common.Interfaces.IIdentityService>();
        var cache = new Mock<BeautyCommerce.Application.Common.Interfaces.ICacheService>();
        var userId = Guid.NewGuid();
        mock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(userId);

        var handler = new RegisterCommandHandler(mock.Object, cache.Object);

        var command = new RegisterCommand { User = new RegisterRequestDto { FirstName = "A", LastName = "B", Email = "a@b.com", Password = "p" } };

        var result = await handler.Handle(command, default);

        result.Should().Be(userId);
        mock.Verify(x => x.RegisterAsync(command.User.FirstName, command.User.LastName, command.User.Email, command.User.Password), Times.Once);
        cache.Verify(x => x.InvalidateTagAsync("Users"), Times.Once);
    }
}
