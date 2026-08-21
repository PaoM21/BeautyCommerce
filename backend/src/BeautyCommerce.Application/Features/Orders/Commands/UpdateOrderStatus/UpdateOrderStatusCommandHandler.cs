using System.Text.Json;
using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Helpers;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
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
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUser;

    public UpdateOrderStatusCommandHandler(
        IApplicationDbContext context,
        ILoyaltyService loyaltyService,
        ICacheService cache,
        IInventoryService inventoryService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _loyaltyService = loyaltyService;
        _cache = cache;
        _inventoryService = inventoryService;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(x => x.Items)
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

        if (previousStatus == OrderStatus.Paid &&
            order.Status == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                await _inventoryService.RegisterEntryAsync(
                    item.ProductVariantId,
                    item.Quantity,
                    $"Restitución por cancelación del pedido {order.OrderNumber}",
                    _currentUser.UserId,
                    cancellationToken);
            }
        }

        if (previousStatus != OrderStatus.Shipped &&
            order.Status == OrderStatus.Shipped)
        {
            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                Type = "OrderShipped",
                Content = JsonSerializer.Serialize(new
                {
                    order.Id,
                    order.OrderNumber,
                    order.UserId
                })
            });
        }

        await _context.SaveChangesAsync(
            cancellationToken);

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
