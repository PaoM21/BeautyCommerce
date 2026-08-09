using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Application.Features.ShoppingCart.Queries.GetCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeautyCommerce.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("items")]
        public async Task<IActionResult> AddItem(AddCartItemDto dto)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var cartId = await _mediator.Send(
            new AddCartItemCommand
            {
                Item = dto,
                UserId = userId
            });

        return Ok(new ApiResponse<Guid>
        {
            Success = true,
            Message = "Producto agregado al carrito.",
            Data = cartId
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var cart = await _mediator.Send(
            new GetCartQuery
            {
                UserId = userId
            });

        return Ok(new ApiResponse<ShoppingCartDto>
        {
            Success = true,
            Message = "Carrito obtenido correctamente.",
            Data = cart
        });
    }
}