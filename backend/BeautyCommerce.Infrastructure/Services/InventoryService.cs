using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;

namespace BeautyCommerce.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _context;

    public InventoryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RegisterEntryAsync(
        Guid productVariantId,
        int quantity,
        string reason,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        await _context.InventoryMovements.AddAsync(
            new InventoryMovement
            {
                ProductVariantId = productVariantId,
                Quantity = quantity,
                IsEntry = true,
                Reason = reason,
                UserId = userId
            },
            cancellationToken);
    }

    public async Task RegisterExitAsync(
        Guid productVariantId,
        int quantity,
        string reason,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        await _context.InventoryMovements.AddAsync(
            new InventoryMovement
            {
                ProductVariantId = productVariantId,
                Quantity = quantity,
                IsEntry = false,
                Reason = reason,
                UserId = userId
            },
            cancellationToken);
    }
}