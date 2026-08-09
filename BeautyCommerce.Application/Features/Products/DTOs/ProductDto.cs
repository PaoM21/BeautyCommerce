using BeautyCommerce.Application.Features.Brands.DTOs;
using BeautyCommerce.Application.Features.Categories.DTOs;
using BeautyCommerce.Application.Features.Products.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }

    public bool IsNew { get; set; }

    public decimal? FromPrice { get; set; }

    public BrandDto Brand { get; set; } = null!;

    public CategoryDto Category { get; set; } = null!;

    public List<ProductImageDto> Images { get; set; } = new();

    public List<ProductVariantDto> Variants { get; set; } = new();
}