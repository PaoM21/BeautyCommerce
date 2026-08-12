using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Brands.Commands.DeleteBrand;

public class DeleteBrandCommandHandler
    : IRequestHandler<DeleteBrandCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteBrandCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(
        DeleteBrandCommand request,
        CancellationToken cancellationToken)
    {
        var brand = await _context.Brands
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (brand == null)
            return false;

        brand.IsActive = false;
        brand.IsDeleted = true;
        brand.DeletedAt = DateTime.UtcNow;
        brand.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}