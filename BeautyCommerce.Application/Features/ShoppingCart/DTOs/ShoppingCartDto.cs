namespace BeautyCommerce.Application.Features.ShoppingCart.DTOs;

public class ShoppingCartDto
{
    public List<ShoppingCartItemDto> Items { get; set; } = new();

    public decimal Total { get; set; }
}