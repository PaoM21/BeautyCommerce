namespace BeautyCommerce.Application.Features.Products.DTOs;

public class UpdateStockDto
{
    public Guid ProductVariantId { get; set; }

    public int Stock { get; set; }
}
