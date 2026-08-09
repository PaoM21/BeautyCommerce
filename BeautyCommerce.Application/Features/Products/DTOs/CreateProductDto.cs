using BeautyCommerce.Application.Features.Products.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid BrandId { get; set; }

    public Guid CategoryId { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsNew { get; set; }

    public CreateVariantDto Variant { get; set; } = new();
}