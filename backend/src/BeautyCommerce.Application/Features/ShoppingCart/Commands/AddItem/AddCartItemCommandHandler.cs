using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;

public class AddCartItemCommandHandler
    : IRequestHandler<AddCartItemCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddCartItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        AddCartItemCommand request,
        CancellationToken cancellationToken)
    {
        // The user must always come from the authenticated JWT.
        var userId = _currentUser.UserId;

        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }
        
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(
                pv => pv.Id == request.Item.ProductVariantId,
                cancellationToken);

        if (variant == null)
        {
            throw new KeyNotFoundException(
                "Product variant not found.");
        }
        
        if (variant.Stock < request.Item.Quantity)
        {
            throw new InvalidOperationException(
                "Insufficient stock for the requested quantity.");
        }
        
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.UserId == userId.Value,
                cancellationToken);
                
        if (cart == null)
        {
            cart = new global::BeautyCommerce.Domain.Entities.ShoppingCart
            {
                UserId = userId.Value
            };

            _context.ShoppingCarts.Add(cart);

            await _context.SaveChangesAsync(cancellationToken);
        }
        
        var existingItem = cart.Items
            .FirstOrDefault(
                i => i.ProductVariantId == variant.Id);

        if (existingItem == null)
        {
            var newItem =
                new global::BeautyCommerce.Domain.Entities.ShoppingCartItem
                {
                    ShoppingCartId = cart.Id,
                    ProductVariantId = variant.Id,
                    Quantity = request.Item.Quantity,
                    UnitPrice = variant.Price
                };

            cart.Items.Add(newItem);
            _context.ShoppingCartItems.Add(newItem);
        }
        else
        {
            existingItem.Quantity += request.Item.Quantity;
            existingItem.UnitPrice = variant.Price;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return cart.Id;
    }
}