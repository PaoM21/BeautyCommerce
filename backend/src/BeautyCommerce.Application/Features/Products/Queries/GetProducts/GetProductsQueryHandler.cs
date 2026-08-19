using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Products.Queries.GetProducts;

public class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductListItemDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .AsQueryable();

        var filter = request.Filter;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();

            query = query.Where(x =>
                x.Name.ToLower().Contains(search));
        }

        if (filter.BrandId.HasValue)
        {
            query = query.Where(x =>
                x.BrandId == filter.BrandId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(x =>
                x.CategoryId == filter.CategoryId.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(x =>
                x.Variants.Any(v =>
                    v.Price >= filter.MinPrice.Value));
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(x =>
                x.Variants.Any(v =>
                    v.Price <= filter.MaxPrice.Value));
        }

        var totalRecords = await query.CountAsync(
            cancellationToken);

        var page = filter.Page < 1
            ? 1
            : filter.Page;

        var pageSize = filter.PageSize <= 0
            ? 12
            : filter.PageSize;


        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new PagedResult<ProductListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize),

            Items = items
        };
    }
}