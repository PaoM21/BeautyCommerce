using BeautyCommerce.Application.Features.ProductVariants.Commands.UpdateStock;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class ProductVariantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductVariantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("stock")]
    public async Task<IActionResult> UpdateStock(UpdateStockCommand command)
    {
        await _mediator.Send(command);

        return NoContent();
    }
}
