using BeautyCommerce.Domain.Enums;
using MediatR;

namespace BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderStatus;

public class UpdateOrderStatusCommand : IRequest<bool>
{
    public Guid OrderId { get; set; }

    public OrderStatus Status { get; set; }
}