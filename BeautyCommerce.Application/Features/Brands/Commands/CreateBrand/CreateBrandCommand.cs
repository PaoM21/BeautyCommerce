using BeautyCommerce.Application.Features.Brands.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Brands.Commands.CreateBrand;

public class CreateBrandCommand : IRequest<Guid>
{
    public CreateBrandDto Brand { get; set; } = new();
}