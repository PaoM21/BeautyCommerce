using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Products.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Products.Queries.SearchProducts;

public class SearchProductsQuery : IRequest<PagedResult<ProductDto>>
{
    public ProductSearchDto Filter { get; set; } = new();
}
