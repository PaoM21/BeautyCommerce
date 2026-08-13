using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public InventoryService(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task RegisterEntryAsync(
        Guid productVariantId,
        int quantity,
        string reason,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "La cantidad debe ser mayor que cero.",
                nameof(quantity));
        }

        var affectedRows = await _context.ProductVariants
            .Where(x => x.Id == productVariantId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.Stock, x => x.Stock + quantity),
                cancellationToken);

        if (affectedRows == 0)
        {
            throw new KeyNotFoundException("La variante del producto no existe.");
        }

        await _context.InventoryMovements.AddAsync(
            new InventoryMovement
            {
                ProductVariantId = productVariantId,
                Quantity = quantity,
                IsEntry = true,
                Reason = reason.Trim(),
                UserId = userId
            },
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateTagAsync("Products");
        await _cache.InvalidateTagAsync("Inventory");
    }

    public async Task RegisterExitAsync(
        Guid productVariantId,
        int quantity,
        string reason,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var accepted = await TryRegisterExitAsync(
            productVariantId, quantity, reason, userId, cancellationToken);

        if (!accepted)
        {
            throw new InvalidOperationException(
                "No hay suficiente stock para realizar la salida.");
        }
    }

    public async Task<bool> TryRegisterExitAsync(
        Guid productVariantId,
        int quantity,
        string reason,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "La cantidad debe ser mayor que cero.",
                nameof(quantity));
        }

        var affectedRows = await _context.ProductVariants
            .Where(x => x.Id == productVariantId && x.Stock >= quantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.Stock, x => x.Stock - quantity),
                cancellationToken);

        if (affectedRows == 0)
        {
            var exists = await _context.ProductVariants
                .AsNoTracking()
                .AnyAsync(x => x.Id == productVariantId, cancellationToken);

            if (!exists)
            {
                throw new KeyNotFoundException("La variante del producto no existe.");
            }
            
            return false;
        }

        await _context.InventoryMovements.AddAsync(
            new InventoryMovement
            {
                ProductVariantId = productVariantId,
                Quantity = quantity,
                IsEntry = false,
                Reason = reason.Trim(),
                UserId = userId
            },
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateTagAsync("Products");
        await _cache.InvalidateTagAsync("Inventory");

        return true;
    }
}
