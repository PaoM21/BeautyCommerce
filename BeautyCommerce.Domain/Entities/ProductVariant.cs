using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string SKU { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal Cost { get; set; }

    public decimal Price { get; set; }

    public decimal? OldPrice { get; set; }

    public int Stock { get; set; }

    public int MinimumStock { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;
}