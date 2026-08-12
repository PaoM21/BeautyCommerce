using BeautyCommerce.Application.Features.Brands.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Brands.Queries.GetAllBrands;

public class GetAllBrandsQuery : IRequest<List<BrandDto>>
{
}