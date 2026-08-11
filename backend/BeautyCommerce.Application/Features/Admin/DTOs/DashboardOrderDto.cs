namespace BeautyCommerce.Application.Features.Admin.DTOs;

public class DashboardOrderDto
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }
}