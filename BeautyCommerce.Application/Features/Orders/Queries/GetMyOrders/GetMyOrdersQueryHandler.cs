using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Orders.Queries.GetMyOrders;

public class GetMyOrdersQueryHandler
    : IRequestHandler<GetMyOrdersQuery, List<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyOrdersQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<OrderDto>> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            throw new UnauthorizedAccessException();

        var orders = await _context.Orders
            .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Where(x => x.UserId == _currentUser.UserId.Value)
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

                ProductName = item.ProductVariant.Product.Name,

                Color = item.ProductVariant.Color,

                Size = item.ProductVariant.Size,

                Quantity = item.Quantity,

                UnitPrice = item.UnitPrice,

                Subtotal = item.Quantity * item.UnitPrice,

                ImageUrl = null

            }).ToList()
        }).ToList();
    }
}
