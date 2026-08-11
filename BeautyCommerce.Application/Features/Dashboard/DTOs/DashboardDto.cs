namespace BeautyCommerce.Application.Features.Dashboard.DTOs;

public class DashboardDto
{
    public int TotalProducts { get; set; }

    public int TotalOrders { get; set; }

    public int TotalCustomers { get; set; }

    public decimal TotalSales { get; set; }

    public decimal SalesThisMonth { get; set; }

    public int PendingOrders { get; set; }

    public int LowStockProducts { get; set; }

    public int OutOfStockProducts { get; set; }
}
