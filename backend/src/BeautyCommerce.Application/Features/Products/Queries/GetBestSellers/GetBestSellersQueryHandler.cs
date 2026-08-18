using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Products.Queries.GetBestSellers;

public class GetBestSellersQueryHandler
    : IRequestHandler<GetBestSellersQuery, List<ProductListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBestSellersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductListItemDto>> Handle(
        GetBestSellersQuery request,
        CancellationToken cancellationToken)
    {
        var topProductIds = await _context.OrderItems
            .Where(oi =>
                oi.Order.Status != OrderStatus.Cancelled &&
                !oi.Order.IsDeleted)
            .GroupBy(oi => oi.ProductVariant.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalSold = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(request.Count)
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken);

        if (topProductIds.Count == 0)
        {
            return new List<ProductListItemDto>();
        }

        var products = await _context.Products
            .AsNoTracking()
            .Where(x => topProductIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new ProductListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Brand = x.Brand.Name,
                Category = x.Category.Name,

                Price = x.Variants.Any()
                    ? x.Variants.Min(v => v.Price)
                    : 0,

                Stock = x.Variants.Sum(v => v.Stock),

                Image = x.Images
                    .Where(i => i.IsPrimary)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
                    ?? x.Images
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),

                AverageRating = _context.Reviews
                    .Where(r => r.ProductId == x.Id)
                    .Average(r => (decimal?)r.Rating),

                ReviewCount = _context.Reviews
                    .Count(r => r.ProductId == x.Id),

                DefaultVariantId = x.Variants
                    .OrderByDescending(v => v.Stock > 0)
                    .ThenBy(v => v.Price)
                    .Select(v => (Guid?)v.Id)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return topProductIds
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Select(p => p!)
            .ToList();
    }
}
