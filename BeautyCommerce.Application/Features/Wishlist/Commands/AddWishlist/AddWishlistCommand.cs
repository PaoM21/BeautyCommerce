using MediatR;

namespace BeautyCommerce.Application.Features.Wishlist.Commands.AddWishlist;

public class AddWishlistCommand : IRequest
{
    public Guid ProductId { get; set; }
}
