using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using MediatR;

namespace BeautyCommerce.Application.Features.Brands.Commands.CreateBrand;

public class CreateBrandCommandHandler
    : IRequestHandler<CreateBrandCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public CreateBrandCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Guid> Handle(
        CreateBrandCommand request,
        CancellationToken cancellationToken)
    {
        var brand = new Brand
        {
            Name = request.Brand.Name,
            Description = request.Brand.Description,
            LogoUrl = request.Brand.LogoUrl
        };

        await _context.Brands.AddAsync(brand, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateTagAsync("Brands");

        return brand.Id;
    }
}