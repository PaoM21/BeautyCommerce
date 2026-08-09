using MediatR;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;

namespace BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;

public class AddCartItemCommand : IRequest<Guid>
{
    public AddCartItemDto Item { get; set; } = null!;

    // Optional: controller/tests may set this before sending
    public Guid? UserId { get; set; }
}
