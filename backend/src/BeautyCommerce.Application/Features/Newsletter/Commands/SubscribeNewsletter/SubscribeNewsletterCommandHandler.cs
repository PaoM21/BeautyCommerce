using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Newsletter.Commands.SubscribeNewsletter;

public class SubscribeNewsletterCommandHandler
    : IRequestHandler<SubscribeNewsletterCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public SubscribeNewsletterCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(
        SubscribeNewsletterCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new BadRequestException(
                "Ingresa un correo electrónico válido.");
        }

        var alreadySubscribed = await _context.NewsletterSubscribers
            .AnyAsync(x => x.Email == email, cancellationToken);

        if (alreadySubscribed)
        {
            return Unit.Value;
        }

        _context.NewsletterSubscribers.Add(new NewsletterSubscriber
        {
            Email = email
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
