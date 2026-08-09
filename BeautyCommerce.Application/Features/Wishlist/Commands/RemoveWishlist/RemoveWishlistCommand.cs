using MediatR;

namespace BeautyCommerce.Application.Features.Wishlist.Commands.RemoveWishlist;

public class RemoveWishlistCommand : IRequest
{
    public Guid ProductId { get; set; }
}
