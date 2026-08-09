using BeautyCommerce.Application.Features.Authentication.Commands.Login;
using BeautyCommerce.Application.Features.Authentication.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var userId = await _mediator.Send(command);

        return Ok(new
        {
            Success = true,
            UserId = userId,
            Message = "Usuario registrado correctamente."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}