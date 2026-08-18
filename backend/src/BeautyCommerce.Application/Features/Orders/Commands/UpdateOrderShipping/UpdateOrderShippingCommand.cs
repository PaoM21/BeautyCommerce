using MediatR;

namespace BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderShipping;

public class UpdateOrderShippingCommand : IRequest<bool>
{
    public Guid OrderId { get; set; }

    public string Carrier { get; set; } = string.Empty;

    public string TrackingNumber { get; set; } = string.Empty;
}
