using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Inventory.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Inventory.Queries.GetInventory;

public class GetInventoryQueryHandler
    : IRequestHandler<GetInventoryQuery, List<InventoryItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryItemDto>> Handle(
        GetInventoryQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.ProductVariants
            .AsNoTracking()
            .Include(x => x.Product)
            .OrderBy(x => x.Product.Name)
            .ThenBy(x => x.SKU)
            .Select(x => new InventoryItemDto
            {
                ProductVariantId = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                SKU = x.SKU,
                Barcode = x.Barcode,
                Color = x.Color,
                Size = x.Size,
                Price = x.Price,
                Stock = x.Stock,
                MinimumStock = x.MinimumStock,

                ImageUrl = x.Product.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),

                StockStatus =
                    x.Stock == 0
                        ? "OutOfStock"
                        : x.Stock <= x.MinimumStock
                            ? "LowStock"
                            : "Available"
            })
            .ToListAsync(cancellationToken);
    }
}
