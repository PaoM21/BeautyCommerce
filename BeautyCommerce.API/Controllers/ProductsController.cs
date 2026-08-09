using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Products.Commands.CreateProduct;
using BeautyCommerce.Application.Features.Products.Commands.DeleteProduct;
using BeautyCommerce.Application.Features.Products.Commands.UpdateProduct;
using BeautyCommerce.Application.Features.Products.Commands.UpdateStock;
using BeautyCommerce.Application.Common.Constants;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Application.Features.Products.Queries.GetProducts;
using BeautyCommerce.Application.Features.Products.Queries.GetProductById;
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
    [HttpPut("stock")]
    public async Task<IActionResult> UpdateStock(UpdateStockCommand command)
    {
        await _mediator.Send(command);

        return NoContent();
    }

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