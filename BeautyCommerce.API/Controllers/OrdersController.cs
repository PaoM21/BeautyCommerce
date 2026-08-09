using BeautyCommerce.Application.Common.Constants;
using BeautyCommerce.Application.Features.Orders.Commands.Checkout;
using BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderStatus;
using BeautyCommerce.Application.Features.Orders.Queries.GetOrderById;
using BeautyCommerce.Application.Features.Orders.Queries.GetOrders;
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
        CancellationToken cancellationToken)
    {
        var orderId = await _mediator.Send(
            new CheckoutCommand(),
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



    [Authorize(Roles = Roles.Admin)]
    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus(UpdateOrderStatusCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result)
            return NotFound();

        return Ok(new
        {
            Success = true,
            Message = "Estado actualizado correctamente."
        });
    }
}