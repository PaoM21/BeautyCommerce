namespace BeautyCommerce.Application.Features.Inventory.DTOs;

public class InventoryItemDto
{
    public Guid ProductVariantId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public int MinimumStock { get; set; }

    public string? ImageUrl { get; set; }

    public string StockStatus { get; set; } = string.Empty;
}
