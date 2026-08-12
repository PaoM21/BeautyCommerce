using BeautyCommerce.Application.Common.Constants;
using BeautyCommerce.Application.Features.Inventory.Commands.UpdateStock;
using BeautyCommerce.Application.Features.Inventory.DTOs;
using BeautyCommerce.Application.Features.Inventory.Queries.GetInventory;
using BeautyCommerce.Application.Features.Inventory.Queries.GetInventoryMovements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = Roles.Admin)]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryItemDto>>> Get()
    {
        var result = await _mediator.Send(new GetInventoryQuery());

        return Ok(result);
    }

    [HttpGet("movements")]
    public async Task<ActionResult<List<InventoryMovementDto>>> GetMovements()
    {
        var result = await _mediator.Send(new GetInventoryMovementsQuery());

        return Ok(result);
    }

    [HttpPut("stock")]
    public async Task<IActionResult> UpdateStock([FromBody] UpdateStockCommand command)
    {
        await _mediator.Send(command);

        return NoContent();
    }
}
