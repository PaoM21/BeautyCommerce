namespace BeautyCommerce.Application.Features.ShoppingCart.DTOs;

public class ShoppingCartItemDto
{
    public Guid ProductId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public string? ImageUrl { get; set; }
}