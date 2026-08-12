namespace BeautyCommerce.Application.Common.Models;

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalRecords { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling((double)TotalRecords / PageSize);
}
