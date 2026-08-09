namespace BeautyCommerce.Application.Features.Admin.DTOs;

public class MonthlySalesDto
{
    public string Month { get; set; } = string.Empty;

    public decimal Total { get; set; }
}