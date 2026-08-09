using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Wishlist.DTOs;
using BeautyCommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Wishlist.Commands.AddWishlist;

public class AddWishlistCommandHandler : IRequestHandler<AddWishlistCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddWishlistCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(AddWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (userId == null)
            throw new UnauthorizedAccessException();

        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product == null)
            throw new NotFoundException("Producto no encontrado.");

        var exists = await _context.WishlistItems
            .AnyAsync(x => x.UserId == userId && x.ProductId == request.ProductId, cancellationToken);

        if (exists)
            return;

        var item = new WishlistItem
        {
            UserId = userId.Value,
            ProductId = request.ProductId
        };

        _context.WishlistItems.Add(item);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
