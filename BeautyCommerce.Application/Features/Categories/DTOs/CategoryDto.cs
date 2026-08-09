namespace BeautyCommerce.Application.Features.Categories.DTOs;

public class CategoryDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public Guid? ParentCategoryId { get; set; }

    public bool IsActive { get; set; }
}