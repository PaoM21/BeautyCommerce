using BeautyCommerce.Application.Features.Brands.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Brands.Commands.UpdateBrand;

public class UpdateBrandCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public BrandDto Brand { get; set; } = new();
}