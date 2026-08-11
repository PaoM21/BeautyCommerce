using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Products.Queries.GetProductById;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BeautyCommerce.Application.Features.ProductVariants.Commands.UpdateStock;

public class UpdateStockCommandHandler
    : IRequestHandler<UpdateStockCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateStockCommandHandler> _logger;

    public UpdateStockCommandHandler(IApplicationDbContext context, ICacheService cache, ILogger<UpdateStockCommandHandler> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(
        UpdateStockCommand request,
        CancellationToken cancellationToken)
    {
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(
                x => x.Id == request.Stock.ProductVariantId,
                cancellationToken);

        if (variant == null)
        {
            _logger?.LogWarning("UpdateStock (variant): not found {VariantId}", request.Stock.ProductVariantId);
            throw new Exception("Variante no encontrada.");
        }

        var old = variant.Stock;
        variant.Stock += request.Stock.Quantity;

        if (variant.Stock < 0)
        {
            _logger?.LogWarning("UpdateStock (variant): resulting stock negative for {VariantId} ({Stock})", variant.Id, variant.Stock);
            throw new Exception("El stock no puede ser negativo.");
        }

        _logger?.LogInformation("UpdateStock (variant): {VariantId} stock changed {Old} => {New}", variant.Id, old, variant.Stock);

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate product cache for the product that contains this variant
        try
        {
            var productId = variant.ProductId;

            var getProductQuery = new GetProductByIdQuery { Id = productId };

            var cacheKey = $"{typeof(GetProductByIdQuery).FullName}:{JsonSerializer.Serialize(getProductQuery)}";

            await _cache.RemoveAsync(cacheKey);

            _logger?.LogInformation("Product cache invalidated for {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to invalidate cache for variant {VariantId}", variant.Id);
        }
    }
}
