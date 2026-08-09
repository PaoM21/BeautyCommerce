namespace BeautyCommerce.Application.Common.Models;

public class ProductFilter
{
    public string? Search { get; set; }

    public Guid? BrandId { get; set; }

    public Guid? CategoryId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool? Featured { get; set; }
}