using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Brands.Commands.UpdateBrand;

public class UpdateBrandCommandHandler
    : IRequestHandler<UpdateBrandCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public UpdateBrandCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> Handle(
        UpdateBrandCommand request,
        CancellationToken cancellationToken)
    {
        var brand = await _context.Brands
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (brand == null)
            return false;

        brand.Name = request.Brand.Name;
        brand.Description = request.Brand.Description;
        brand.LogoUrl = request.Brand.LogoUrl;

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateTagAsync("Brands");

        return true;
    }
}