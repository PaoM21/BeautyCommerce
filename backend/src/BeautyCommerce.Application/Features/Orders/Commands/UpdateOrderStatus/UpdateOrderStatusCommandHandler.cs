using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Helpers;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Orders.Commands.UpdateOrderStatus;

public class UpdateOrderStatusCommandHandler
    : IRequestHandler<UpdateOrderStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILoyaltyService _loyaltyService;
    private readonly ICacheService _cache;

    public UpdateOrderStatusCommandHandler(
        IApplicationDbContext context,
        ILoyaltyService loyaltyService,
        ICacheService cache)
    {
        _context = context;
        _loyaltyService = loyaltyService;
        _cache = cache;
    }

    public async Task<bool> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(
                x => x.Id == request.OrderId,
                cancellationToken);

        if (order == null)
            return false;

        if (!OrderStatusValidator.IsValidTransition(
                order.Status,
                request.Status))
        {
            throw new BadRequestException(
                "Cambio de estado no permitido.");
        }

        var previousStatus = order.Status;

        order.Status = request.Status;

        await _context.SaveChangesAsync(
            cancellationToken);

        // Invalida todas las consultas cacheadas del feature Orders (listados y
        // detalles, tanto administrativos como del cliente), ya que todas
        // comparten el tag "Orders" generado por CachingBehavior.
        await _cache.InvalidateTagAsync("Orders");

        if (previousStatus != OrderStatus.Delivered &&
            order.Status == OrderStatus.Delivered)
        {
            await _loyaltyService.AwardPointsForOrderAsync(
                order.Id,
                cancellationToken);
        }

        return true;
    }
}
