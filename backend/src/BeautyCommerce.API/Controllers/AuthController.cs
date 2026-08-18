using BeautyCommerce.Application.Features.Authentication.Commands.ForgotPassword;
using BeautyCommerce.Application.Features.Authentication.Commands.Login;
using BeautyCommerce.Application.Features.Authentication.Commands.Register;
using BeautyCommerce.Application.Features.Authentication.Commands.ResetPassword;
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command)
    {
        await _mediator.Send(command);

        // Misma respuesta exista o no el correo — evita revelar qué
        // correos están registrados en el sistema.
        return Ok(new
        {
            Success = true,
            Message = "Si el correo está registrado, te enviamos un enlace para restablecer tu contraseña."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command)
    {
        await _mediator.Send(command);

        return Ok(new
        {
            Success = true,
            Message = "Tu contraseña fue actualizada correctamente."
        });
    }
}