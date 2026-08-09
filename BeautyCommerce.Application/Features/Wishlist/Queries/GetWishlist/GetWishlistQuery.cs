using BeautyCommerce.Application.Features.Wishlist.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Wishlist.Queries.GetWishlist;

public class GetWishlistQuery : IRequest<List<WishlistItemDto>>
{
}
