using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Helpers;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.Queries.GetAdminOrderById;
using BeautyCommerce.Application.Features.Orders.Queries.GetAllOrders;
using BeautyCommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        // Invalidar cache del detalle administrativo
        var adminOrderCacheKey =
            $"{typeof(GetAdminOrderByIdQuery).FullName}:" +
            $"{JsonSerializer.Serialize(new GetAdminOrderByIdQuery { Id = order.Id })}";

        await _cache.RemoveAsync(adminOrderCacheKey);

        // Invalidar cache del listado administrativo
        var adminOrdersCacheKey =
            $"{typeof(GetAllOrdersQuery).FullName}:" +
            $"{JsonSerializer.Serialize(new GetAllOrdersQuery())}";

        await _cache.RemoveAsync(adminOrdersCacheKey);

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
