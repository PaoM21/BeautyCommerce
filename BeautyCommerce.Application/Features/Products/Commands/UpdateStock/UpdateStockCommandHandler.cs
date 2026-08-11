using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeautyCommerce.Application.Features.Products.Commands.UpdateStock;

public class UpdateStockCommandHandler
    : IRequestHandler<UpdateStockCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateStockCommandHandler> _logger;

    public UpdateStockCommandHandler(IApplicationDbContext context, ILogger<UpdateStockCommandHandler> logger)
    {
        _context = context;
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
    }
}
