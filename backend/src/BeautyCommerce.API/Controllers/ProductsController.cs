using BeautyCommerce.Application.Common.Constants;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Products.Commands.CreateProduct;
using BeautyCommerce.Application.Features.Products.Commands.DeleteProduct;
using BeautyCommerce.Application.Features.Products.Commands.UpdateProduct;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Application.Features.Products.Queries.GetProducts;
using BeautyCommerce.Application.Features.Products.Queries.GetProductById;
using BeautyCommerce.Application.Features.Products.Queries.GetBestSellers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductCommand command)
    {
        var id = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            id);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(
        [FromQuery] ProductFilterDto filter)
    {
        var result = await _mediator.Send(
            new GetProductsQuery
            {
                Filter = filter
            });

        return Ok(result);
    }

    [HttpGet("best-sellers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBestSellers([FromQuery] int count)
    {
        var result = await _mediator.Send(
            new GetBestSellersQuery
            {
                Count = count > 0 ? count : 8
            });

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery
        {
            Id = id
        });

        return Ok(result);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
    Guid id,
    UpdateProductCommand command)
    {
        command.Id = id;

        await _mediator.Send(command);

        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteProductCommand
        {
            Id = id
        });

        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Producto no encontrado."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Producto desactivado correctamente."
        });
    }
}