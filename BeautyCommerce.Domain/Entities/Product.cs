using BeautyCommerce.Domain.Base;
using BeautyCommerce.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid BrandId { get; set; }

    public Brand Brand { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public bool IsFeatured { get; set; }

    public bool IsNew { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}