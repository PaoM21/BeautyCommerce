using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Brands.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Brands.Queries.GetDeletedBrands;

public class GetDeletedBrandsQueryHandler
    : IRequestHandler<GetDeletedBrandsQuery, List<BrandDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDeletedBrandsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrandDto>> Handle(
        GetDeletedBrandsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Brands
            .IgnoreQueryFilters()
            .Where(x => x.IsDeleted)
            .Select(x => new BrandDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}