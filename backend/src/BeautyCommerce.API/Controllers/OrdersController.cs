using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Application.Features.Orders.Queries.GetOrderById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        CheckoutCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = await _mediator.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            orderId
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var result = await _mediator.Send(
            new BeautyCommerce.Application.Features.Orders.Queries.GetMyOrders.GetMyOrdersQuery());

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var result = await _mediator.Send(
            new GetOrderByIdQuery
            {
                Id = id
            });

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}