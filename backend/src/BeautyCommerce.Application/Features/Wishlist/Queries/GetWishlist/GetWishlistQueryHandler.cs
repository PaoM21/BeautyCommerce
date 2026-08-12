using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Wishlist.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Wishlist.Queries.GetWishlist;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, List<WishlistItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetWishlistQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<WishlistItemDto>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (userId == null)
            return new List<WishlistItemDto>();

        var items = await _context.WishlistItems
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        return items.Select(i => new WishlistItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            Price = i.Product.Variants.Any() ? i.Product.Variants.Min(v => v.Price) : 0,
            ImageUrl = i.Product.Images.FirstOrDefault()?.ImageUrl
        }).ToList();
    }
}
