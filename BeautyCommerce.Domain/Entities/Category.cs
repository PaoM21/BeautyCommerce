using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class Category : BaseEntity
{
    // ============================
    // Información General
    // ============================

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    // ============================
    // Jerarquía
    // ============================

    public Guid? ParentCategoryId { get; set; }

    public Category? ParentCategory { get; set; }

    public ICollection<Category> Children { get; set; }
        = new List<Category>();

    // ============================
    // Navegación
    // ============================

    public ICollection<Product> Products { get; set; }
        = new List<Product>();
}