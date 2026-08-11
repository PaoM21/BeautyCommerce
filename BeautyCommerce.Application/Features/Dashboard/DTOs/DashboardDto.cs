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

    public List<DashboardOrderDto> LastOrders { get; set; } = [];

    public List<MonthlySalesDto> SalesByMonth { get; set; } = [];
}

public class DashboardOrderDto
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class MonthlySalesDto
{
    public string Month { get; set; } = string.Empty;

    public decimal Total { get; set; }
}
