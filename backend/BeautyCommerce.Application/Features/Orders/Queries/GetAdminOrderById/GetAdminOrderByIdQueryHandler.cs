using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Orders.Queries.GetAdminOrderById;

public class GetAdminOrderByIdQueryHandler
    : IRequestHandler<GetAdminOrderByIdQuery, OrderDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAdminOrderByIdQueryHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderDto?> Handle(
        GetAdminOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new OrderDto
            {
                Id = x.Id,
                UserId = x.UserId,
                OrderNumber = x.OrderNumber,
                OrderDate = x.OrderDate,
                Status = x.Status.ToString(),
                Total = x.Total,
                TransactionId = x.TransactionId,

                Items = x.Items.Select(item => new OrderItemDto
                {
                    Id = item.Id,
                    ProductVariantId = item.ProductVariantId,

                    ProductName =
                        item.ProductVariant.Product.Name,

                    Color =
                        item.ProductVariant.Color,

                    Size =
                        item.ProductVariant.Size,

                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Subtotal =
                        item.Quantity * item.UnitPrice,

                    ImageUrl =
                        item.ProductVariant.Product.Images
                            .Where(image => image.IsPrimary)
                            .Select(image => image.ImageUrl)
                            .FirstOrDefault()
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
