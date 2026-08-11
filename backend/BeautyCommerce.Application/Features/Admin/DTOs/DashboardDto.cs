namespace BeautyCommerce.Application.Features.Admin.DTOs;

public class DashboardDto
{
    public int TotalProducts { get; set; }

    public int TotalBrands { get; set; }

    public int TotalCategories { get; set; }

    public int TotalOrders { get; set; }

    public int TotalCustomers { get; set; }

    public decimal TotalSales { get; set; }
}
