using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class InventoryMovement : BaseEntity
{
    public Guid ProductVariantId { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;

    public int Quantity { get; set; }

    // Entrada = suma stock
    // Salida = resta stock
    public bool IsEntry { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid? UserId { get; set; }
}