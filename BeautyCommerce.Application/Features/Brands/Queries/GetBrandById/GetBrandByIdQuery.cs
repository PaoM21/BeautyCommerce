using BeautyCommerce.Application.Features.Brands.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Brands.Queries.GetBrandById;

public class GetBrandByIdQuery : IRequest<BrandDto?>
{
    public Guid Id { get; set; }
}