using BeautyCommerce.Domain.Base;
using BeautyCommerce.Domain.Enums;

namespace BeautyCommerce.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public decimal SubTotal { get; set; }

    public decimal ShippingCost { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public string? TransactionId { get; set; }

    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();
}