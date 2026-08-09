using MediatR;

namespace BeautyCommerce.Application.Features.Orders.Commands.Checkout;

public class CheckoutCommand : IRequest<Guid>
{
}