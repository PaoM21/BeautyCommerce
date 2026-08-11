using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Products.Queries.GetProductById;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BeautyCommerce.Application.Features.Products.Commands.UpdateStock;

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
            _logger?.LogWarning("UpdateStock: variant not found {VariantId}", request.Stock.ProductVariantId);
            throw new NotFoundException("La variante no existe.");
        }

        var old = variant.Stock;
        variant.Stock = request.Stock.Stock;

        _logger?.LogInformation("UpdateStock: variant {VariantId} stock changed {Old} => {New}", variant.Id, old, variant.Stock);

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache for the product detail
        try
        {
            var productId = variant.ProductId;

            var getProductQuery = new GetProductByIdQuery { Id = productId };

            var cacheKey = $"{typeof(GetProductByIdQuery).FullName}:{JsonSerializer.Serialize(getProductQuery)}";

            await _cache.RemoveAsync(cacheKey);

            _logger?.LogInformation("Cache invalidated for product {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed invalidating cache for variant {VariantId}", variant.Id);
        }
    }
}
