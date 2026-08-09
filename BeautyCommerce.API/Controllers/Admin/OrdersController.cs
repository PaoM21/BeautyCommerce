using BeautyCommerce.Application.Common.Constants;
using BeautyCommerce.Application.Features.Orders.Queries.GetAllOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
[ApiController]
[Route("api/admin/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllOrdersQuery());

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAdminOrderByIdQuery
            {
                Id = id
            },
            cancellationToken);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
