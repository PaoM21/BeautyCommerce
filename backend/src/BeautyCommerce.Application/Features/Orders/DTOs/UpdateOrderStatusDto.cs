using BeautyCommerce.Domain.Enums;

namespace BeautyCommerce.Application.Features.Orders.DTOs;

public class UpdateOrderStatusDto
{
    public Guid OrderId { get; set; }

    public OrderStatus Status { get; set; }
}
