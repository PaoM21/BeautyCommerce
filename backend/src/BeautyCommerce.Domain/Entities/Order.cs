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

    public string ShippingRecipientName { get; set; } = string.Empty;

    public string ShippingPhone { get; set; } = string.Empty;

    public string ShippingAddressLine { get; set; } = string.Empty;

    public string ShippingCity { get; set; } = string.Empty;

    public string ShippingDepartment { get; set; } = string.Empty;

    public string? Carrier { get; set; }

    public string? TrackingNumber { get; set; }

    public DateTime? ShippedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();
}