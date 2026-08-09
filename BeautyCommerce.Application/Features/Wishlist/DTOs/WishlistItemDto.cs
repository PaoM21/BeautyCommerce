namespace BeautyCommerce.Application.Features.Wishlist.DTOs;

public class WishlistItemDto
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }
}
