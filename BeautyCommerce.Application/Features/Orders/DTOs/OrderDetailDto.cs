using BeautyCommerce.Domain.Enums;

namespace BeautyCommerce.Application.Features.Orders.DTOs;

public class OrderDetailDto
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrderStatus Status { get; set; }

    public decimal SubTotal { get; set; }

    public decimal ShippingCost { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}