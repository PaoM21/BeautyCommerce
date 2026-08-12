namespace BeautyCommerce.Application.Features.Products.DTOs;

public class UpdateProductDto
{
    public string SKU { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid BrandId { get; set; }

    public Guid CategoryId { get; set; }

    public decimal Cost { get; set; }

    public decimal Price { get; set; }

    public decimal? OldPrice { get; set; }

    public int Stock { get; set; }

    public int MinimumStock { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsNew { get; set; }
}