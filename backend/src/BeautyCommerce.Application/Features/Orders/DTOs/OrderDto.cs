namespace BeautyCommerce.Application.Features.Orders.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal SubTotal { get; set; }

    public decimal ShippingCost { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public string? TransactionId { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}
