using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly IMediator _mediator;

    public CheckoutController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(
        CancellationToken cancellationToken)
    {
        var orderId = await _mediator.Send(
            new CheckoutCommand(),
            cancellationToken);

        return Ok(new
        {
            Success = true,
            OrderId = orderId,
            Message = "Pedido creado correctamente."
        });
    }
}
