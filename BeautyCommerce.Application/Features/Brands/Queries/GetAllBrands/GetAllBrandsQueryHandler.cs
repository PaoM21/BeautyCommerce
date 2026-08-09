using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Brands.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Brands.Queries.GetAllBrands;

public class GetAllBrandsQueryHandler
    : IRequestHandler<GetAllBrandsQuery, List<BrandDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllBrandsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrandDto>> Handle(
        GetAllBrandsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Brands
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new BrandDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                LogoUrl = x.LogoUrl,
                IsActive = x.IsActive
            })

            .ToListAsync(cancellationToken);
    }
}