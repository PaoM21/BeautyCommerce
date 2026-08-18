using MediatR;

namespace BeautyCommerce.Application.Features.Newsletter.Commands.SubscribeNewsletter;

public class SubscribeNewsletterCommand : IRequest<Unit>
{
    public string Email { get; set; } = string.Empty;
}
