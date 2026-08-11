using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Application.Features.Brands.DTOs;
using BeautyCommerce.Application.Features.Categories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Products.Queries.SearchProducts;

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, PagedResult<ProductDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductDto>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter;

        var query = _context.Products
            .AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Include(x => x.Variants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.Name.Contains(filter.Search));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == filter.CategoryId.Value);
        }

        if (filter.BrandId.HasValue)
        {
            query = query.Where(x => x.BrandId == filter.BrandId.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(x => x.Variants.Any(v => v.Price >= filter.MinPrice.Value));
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Variants.Any(v => v.Price <= filter.MaxPrice.Value));
        }

        if (filter.InStock == true)
        {
            query = query.Where(x => x.Variants.Any(v => v.Stock > 0));
        }

        query = filter.SortBy switch
        {
            "price" => filter.Descending
                ? query.OrderByDescending(x => x.Variants.Min(v => v.Price))
                : query.OrderBy(x => x.Variants.Min(v => v.Price)),

            "name" => filter.Descending
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name),

            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ProductDto>
        {
            Items = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                ShortDescription = p.ShortDescription,
                IsFeatured = p.IsFeatured,
                IsNew = p.IsNew,
                FromPrice = p.Variants.Any() ? p.Variants.Min(v => v.Price) : (decimal?)null,
                Brand = new BrandDto
                {
                    Id = p.Brand.Id,
                    Name = p.Brand.Name,
                    Description = p.Brand.Description,
                    LogoUrl = p.Brand.LogoUrl
                },
                Category = new CategoryDto
                {
                    Id = p.Category.Id,
                    Name = p.Category.Name,
                    Slug = p.Category.Slug,
                    Description = p.Category.Description,
                    ImageUrl = p.Category.ImageUrl,
                    ParentCategoryId = p.Category.ParentCategoryId,
                    IsActive = p.Category.IsActive
                },
                Images = p.Images.Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    Url = i.ImageUrl
                }).ToList(),
                Variants = p.Variants.Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Name = v.SKU,
                    Price = v.Price,
                    Stock = v.Stock
                }).ToList()
            }).ToList(),

            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize)
        };

        return result;
    }
}
