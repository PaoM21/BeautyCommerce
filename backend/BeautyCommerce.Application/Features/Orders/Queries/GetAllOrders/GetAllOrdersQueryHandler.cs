using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Orders.Queries.GetAllOrders;

public class GetAllOrdersQueryHandler
    : IRequestHandler<GetAllOrdersQuery, List<OrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllOrdersQueryHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderDto>> Handle(
        GetAllOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .OrderByDescending(x => x.OrderDate)
            .ToListAsync(cancellationToken);

        return orders.Select(order => new OrderDto
        {
            Id = order.Id,

            UserId = order.UserId,

            OrderNumber = order.OrderNumber,

            OrderDate = order.OrderDate,

            Status = order.Status.ToString(),

            SubTotal = order.SubTotal,

            ShippingCost = order.ShippingCost,

            Tax = order.Tax,

            Total = order.Total,

            TransactionId = order.TransactionId,

            Items = order.Items.Select(item => new OrderItemDto
            {
                Id = item.Id,

                ProductVariantId = item.ProductVariantId,

                ProductName = item.ProductVariant?.Product?.Name ?? string.Empty,

                Color = item.ProductVariant?.Color ?? string.Empty,

                Size = item.ProductVariant?.Size ?? string.Empty,

                Quantity = item.Quantity,

                UnitPrice = item.UnitPrice,

                Subtotal = item.Quantity * item.UnitPrice,

                ImageUrl = null

            }).ToList()
        }).ToList();
    }
}
