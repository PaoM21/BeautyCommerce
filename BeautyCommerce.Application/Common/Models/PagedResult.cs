namespace BeautyCommerce.Application.Common.Models;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalRecords { get; set; }

    // keep TotalItems alias for compatibility
    public int TotalItems
    {
        get => TotalRecords;
        set => TotalRecords = value;
    }

    public int TotalPages { get; set; }
}
