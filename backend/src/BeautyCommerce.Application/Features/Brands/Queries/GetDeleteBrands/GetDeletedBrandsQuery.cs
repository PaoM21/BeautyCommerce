using BeautyCommerce.Application.Features.Brands.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Brands.Queries.GetDeletedBrands;

public record GetDeletedBrandsQuery() : IRequest<List<BrandDto>>;