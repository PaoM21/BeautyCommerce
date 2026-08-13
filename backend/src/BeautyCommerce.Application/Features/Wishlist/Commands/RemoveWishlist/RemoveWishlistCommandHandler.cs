using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Wishlist.Commands.RemoveWishlist;

public class RemoveWishlistCommandHandler : IRequestHandler<RemoveWishlistCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RemoveWishlistCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (userId == null)
            throw new UnauthorizedAccessException();

        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == request.ProductId, cancellationToken);

        if (item == null)
            return;
            
        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        item.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
