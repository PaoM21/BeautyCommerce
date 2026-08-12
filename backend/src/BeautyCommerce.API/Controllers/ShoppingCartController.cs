using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.Clear;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.RemoveItem;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.UpdateItem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeautyCommerce.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShoppingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ShoppingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem(AddCartItemCommand command)
        {
            var userId = Guid.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
            command.UserId = userId;

            await _mediator.Send(command);

            return Ok();
        }

        [HttpPut("items")]
        public async Task<IActionResult> UpdateItem(UpdateCartItemCommand command)
        {
            await _mediator.Send(command);

            return NoContent();
        }

        [HttpDelete("items/{productVariantId:guid}")]
        public async Task<IActionResult> Remove(Guid productVariantId)
        {
            await _mediator.Send(new RemoveCartItemCommand
            {
                ProductVariantId = productVariantId
            });

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> Clear()
        {
            await _mediator.Send(new ClearCartCommand());

            return NoContent();
        }
    }
}

