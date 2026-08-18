using BeautyCommerce.Application.Features.Products.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Products.Queries.GetBestSellers;

public class GetBestSellersQuery : IRequest<List<ProductListItemDto>>
{
    public int Count { get; set; } = 8;
}
