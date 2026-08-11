using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Inventory.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Inventory.Queries.GetInventoryMovements;

public class GetInventoryMovementsQueryHandler
    : IRequestHandler<GetInventoryMovementsQuery, List<InventoryMovementDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryMovementsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryMovementDto>> Handle(
        GetInventoryMovementsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.InventoryMovements
            .AsNoTracking()
            .Include(x => x.ProductVariant)
            .ThenInclude(x => x.Product)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new InventoryMovementDto
            {
                Id = x.Id,
                ProductVariantId = x.ProductVariantId,
                ProductName = x.ProductVariant.Product.Name,
                SKU = x.ProductVariant.SKU,
                Color = x.ProductVariant.Color,
                Size = x.ProductVariant.Size,
                Quantity = x.Quantity,
                IsEntry = x.IsEntry,
                Reason = x.Reason,
                UserId = x.UserId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
