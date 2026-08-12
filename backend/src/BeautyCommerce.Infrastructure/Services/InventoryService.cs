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

        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(x => x.Id == productVariantId, cancellationToken);

        if (variant == null)
        {
            throw new KeyNotFoundException("La variante del producto no existe.");
        }

        variant.Stock += quantity;

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
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "La cantidad debe ser mayor que cero.",
                nameof(quantity));
        }

        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(x => x.Id == productVariantId, cancellationToken);

        if (variant == null)
        {
            throw new KeyNotFoundException("La variante del producto no existe.");
        }

        if (variant.Stock < quantity)
        {
            throw new InvalidOperationException(
                "No hay suficiente stock para realizar la salida.");
        }

        variant.Stock -= quantity;

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
    }
}
