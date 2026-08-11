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
        var products = await _context.Products
            .AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ProductListItemDto>
        {
            Page = 1,
            PageSize = products.Count,
            TotalRecords = products.Count,
            TotalPages = 1,
            Items = products.Select(x => new ProductListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Brand = x.Brand.Name,
                Category = x.Category.Name,

                Price = 0,
                Stock = 0,
                Image = null
            }).ToList()
        };

        return result;
    }
}