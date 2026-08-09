using BeautyCommerce.Application.Features.Wishlist.Commands.AddWishlist;
using BeautyCommerce.Application.Features.Wishlist.Queries.GetWishlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddWishlistCommand command)
    {
        await _mediator.Send(command);

        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _mediator.Send(new GetWishlistQuery());

        return Ok(items);
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Delete(Guid productId)
    {
        var command = new BeautyCommerce.Application.Features.Wishlist.Commands.RemoveWishlist.RemoveWishlistCommand
        {
            ProductId = productId
        };

        await _mediator.Send(command);

        return NoContent();
    }
}
