namespace BeautyCommerce.Application.Features.Products.DTOs;

public class CreateVariantDto
{
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }
}