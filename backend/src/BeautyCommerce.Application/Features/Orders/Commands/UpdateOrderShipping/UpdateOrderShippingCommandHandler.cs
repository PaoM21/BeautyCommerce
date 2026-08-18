using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderShipping;

public class UpdateOrderShippingCommandHandler
    : IRequestHandler<UpdateOrderShippingCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public UpdateOrderShippingCommandHandler(
        IApplicationDbContext context,
        ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> Handle(
        UpdateOrderShippingCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(
                x => x.Id == request.OrderId,
                cancellationToken);

        if (order == null)
            return false;

        order.Carrier = request.Carrier;
        order.TrackingNumber = request.TrackingNumber;
        order.ShippedAt ??= DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateTagAsync("Orders");

        return true;
    }
}
