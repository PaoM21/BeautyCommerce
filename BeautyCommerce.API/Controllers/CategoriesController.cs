using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Categories.Commands.CreateCategory;
using BeautyCommerce.Application.Features.Categories.Commands.DeleteCategory;
using BeautyCommerce.Application.Features.Categories.Commands.UpdateCategory;
using BeautyCommerce.Application.Features.Categories.DTOs;
using BeautyCommerce.Application.Features.Categories.Queries.GetAllCategories;
using BeautyCommerce.Application.Features.Categories.Queries.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryCommand command)
    {
        var id = await _mediator.Send(command);

        return Ok(new ApiResponse<Guid>
        {
            Success = true,
            Message = "Categoría creada correctamente.",
            Data = id
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _mediator.Send(new GetAllCategoriesQuery());

        return Ok(new ApiResponse<List<CategoryDto>>
        {
            Success = true,
            Message = "Categorías obtenidas correctamente.",
            Data = categories
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var category = await _mediator.Send(new GetCategoryByIdQuery
        {
            Id = id
        });

        if (category == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Categoría no encontrada."
            });
        }

        return Ok(new ApiResponse<CategoryDto>
        {
            Success = true,
            Message = "Categoría obtenida correctamente.",
            Data = category
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryDto dto)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand
        {
            Id = id,
            Category = dto
        });

        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Categoría no encontrada."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Categoría actualizada correctamente."
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand
        {
            Id = id
        });

        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Categoría no encontrada."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Categoría eliminada correctamente."
        });
    }
}