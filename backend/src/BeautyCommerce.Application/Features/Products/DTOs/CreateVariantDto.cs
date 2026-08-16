namespace BeautyCommerce.Application.Features.Products.DTOs;

public class CreateVariantDto
{
    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;
}