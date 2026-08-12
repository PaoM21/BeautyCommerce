namespace BeautyCommerce.Application.Features.Products.DTOs;

public class ProductSearchDto
{
    public string? Search { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? BrandId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool? InStock { get; set; }

    public string? SortBy { get; set; }

    public bool Descending { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 12;
}
