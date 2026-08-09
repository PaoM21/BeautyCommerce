namespace BeautyCommerce.Application.Features.ShoppingCart.DTOs;

public class AddCartItemDto
{
    public Guid ProductVariantId { get; set; }

    public int Quantity { get; set; }
}