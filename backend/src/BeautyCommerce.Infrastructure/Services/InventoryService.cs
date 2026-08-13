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

        // Atomic increment: a single UPDATE, not a read-then-write, so two
        // concurrent entries on the same variant can never lose one of them
        // to a stale in-memory value (see InventoryConcurrencyTests).
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

        // Atomic conditional decrement: the "is there enough stock?" check
        // and the decrement happen as one UPDATE ... WHERE Stock >= quantity
        // statement, so two concurrent exits on the same variant can never
        // both read the same stale Stock and both succeed (see
        // InventoryConcurrencyTests / CheckoutConcurrencyTests). Whichever
        // request's UPDATE commits first wins; the second one's WHERE
        // clause re-evaluates against the now-reduced Stock and affects 0
        // rows.
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

            // Not enough stock is a normal business outcome for callers
            // like checkout, not an exceptional one — they decide how to
            // react (RegisterExitAsync above turns it back into an
            // exception for the admin "salida manual" flow).
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
