namespace BeautyCommerce.Application.Features.ProductVariants.DTOs;

public class UpdateStockDto
{
    public Guid ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public string Reason { get; set; } = string.Empty;
}
