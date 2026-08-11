namespace BeautyCommerce.Application.Features.Brands.DTOs;

public class CreateBrandDto
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string LogoUrl { get; set; } = string.Empty;
}