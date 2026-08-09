using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Products.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Products.Queries.GetProducts;

public class GetProductsQuery : IRequest<PagedResult<ProductListItemDto>>
{
    public ProductFilterDto Filter { get; set; } = new();
}