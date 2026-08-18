using MediatR;

namespace BeautyCommerce.Application.Features.Orders.Commands.Checkout;

public class CheckoutCommand : IRequest<Guid>
{
    public string RecipientName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string AddressLine { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;
}