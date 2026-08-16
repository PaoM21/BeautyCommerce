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
            
        var existing = await _context.WishlistItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.ProductId == request.ProductId,
                cancellationToken);

        if (existing != null)
        {
            if (!existing.IsDeleted)
                return;

            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;

            await _context.SaveChangesAsync(cancellationToken);

            return;
        }

        var item = new WishlistItem
        {
            UserId = userId.Value,
            ProductId = request.ProductId
        };

        _context.WishlistItems.Add(item);

        var transaction = _context.Database.CurrentTransaction;
        var useSavepoint = transaction?.SupportsSavepoints ?? false;

        if (useSavepoint)
            await transaction!.CreateSavepointAsync("BeforeWishlistItemInsert", cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (useSavepoint)
                await transaction!.RollbackToSavepointAsync("BeforeWishlistItemInsert", cancellationToken);

            _context.WishlistItems.Remove(item);

            var alreadyExists = await _context.WishlistItems
                .IgnoreQueryFilters()
                .AnyAsync(
                    x => x.UserId == userId.Value && x.ProductId == request.ProductId,
                    cancellationToken);

            if (!alreadyExists)
                throw;
        }
    }
}
