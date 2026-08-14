using System.Text.Json.Serialization;

namespace BeautyCommerce.Application.Features.Products.DTOs;

public class ProductVariantDto
{
    public Guid Id { get; set; }

    [JsonPropertyName("sku")]
    public string SKU { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;
}
