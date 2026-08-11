namespace BeautyCommerce.Application.Features.Inventory.DTOs;

public class InventoryMovementDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public bool IsEntry { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
