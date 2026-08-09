using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Brands.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Brands.Queries.GetBrandById;

public class GetBrandByIdQueryHandler
    : IRequestHandler<GetBrandByIdQuery, BrandDto?>
{
    private readonly IApplicationDbContext _context;

    public GetBrandByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BrandDto?> Handle(
        GetBrandByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Brands

            .AsNoTracking()

            .Where(x => x.Id == request.Id && x.IsActive)

            .Select(x => new BrandDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                LogoUrl = x.LogoUrl,
                IsActive = x.IsActive
            })

            .FirstOrDefaultAsync(cancellationToken);
    }
}