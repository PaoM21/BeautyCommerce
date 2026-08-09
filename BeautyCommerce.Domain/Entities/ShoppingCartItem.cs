using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class ShoppingCartItem : BaseEntity
{
    public Guid ShoppingCartId { get; set; }

    public ShoppingCart ShoppingCart { get; set; } = null!;

    public Guid ProductVariantId { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}