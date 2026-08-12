using BeautyCommerce.Domain.Base;
using BeautyCommerce.Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string LogoUrl { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}