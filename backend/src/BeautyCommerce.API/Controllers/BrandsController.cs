using BeautyCommerce.API.Common.Controllers;
using BeautyCommerce.Application.Common.Constants;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Brands.Commands.CreateBrand;
using BeautyCommerce.Application.Features.Brands.Commands.DeleteBrand;
using BeautyCommerce.Application.Features.Brands.Commands.UpdateBrand;
using BeautyCommerce.Application.Features.Brands.DTOs;
using BeautyCommerce.Application.Features.Brands.Queries.GetAllBrands;
using BeautyCommerce.Application.Features.Brands.Queries.GetBrandById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BrandsController : ApiControllerBase
{
    public BrandsController(IMediator mediator) : base(mediator)
    {
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateBrandCommand command)
    {
        var id = await Mediator.Send(command);

        return Ok(new ApiResponse<Guid>
        {
            Success = true,
            Message = "Marca creada correctamente.",
            Data = id
        });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var brands = await Mediator.Send(new GetAllBrandsQuery());

        return Ok(new ApiResponse<List<BrandDto>>
        {
            Success = true,
            Message = "Marcas obtenidas correctamente.",
            Data = brands
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var brand = await Mediator.Send(new GetBrandByIdQuery
        {
            Id = id
        });

        if (brand is null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Marca no encontrada."
            });
        }

        return Ok(new ApiResponse<BrandDto>
        {
            Success = true,
            Message = "Marca obtenida correctamente.",
            Data = brand
        });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBrandCommand command)
    {
        command.Id = id;

        var updated = await Mediator.Send(command);

        if (!updated)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Marca no encontrada."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Marca actualizada correctamente."
        });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await Mediator.Send(new DeleteBrandCommand
        {
            Id = id
        });

        if (!deleted)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Marca no encontrada."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Marca eliminada correctamente."
        });
    }
}