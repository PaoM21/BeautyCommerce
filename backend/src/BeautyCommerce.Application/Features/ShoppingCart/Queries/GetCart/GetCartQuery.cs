using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.ShoppingCart.Queries.GetCart;

public class GetCartQuery : IRequest<ShoppingCartDto>
{
    public Guid UserId { get; set; }
}