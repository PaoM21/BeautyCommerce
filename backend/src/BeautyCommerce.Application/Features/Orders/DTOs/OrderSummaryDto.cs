using BeautyCommerce.Domain.Enums;

namespace BeautyCommerce.Application.Features.Orders.DTOs;

public class OrderSummaryDto
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrderStatus Status { get; set; }

    public decimal Total { get; set; }
}