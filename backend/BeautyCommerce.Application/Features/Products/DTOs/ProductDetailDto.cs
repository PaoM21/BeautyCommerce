namespace BeautyCommerce.Application.Features.Products.DTOs;

public class ProductDetailDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public List<ProductVariantDto> Variants { get; set; } = [];

    public List<ProductImageDto> Images { get; set; } = [];
}