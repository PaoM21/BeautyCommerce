using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using MediatR;

namespace BeautyCommerce.Application.Features.Brands.Commands.CreateBrand;

public class CreateBrandCommandHandler
    : IRequestHandler<CreateBrandCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateBrandCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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

        return brand.Id;
    }
}