using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Orders.Queries.GetOrders;

public class GetOrdersQueryHandler
    : IRequestHandler<GetOrdersQuery, List<OrderSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    public GetOrdersQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<OrderSummaryDto>> Handle(
        GetOrdersQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Where(x => x.UserId == _currentUser.UserId!.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new OrderSummaryDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                CreatedAt = x.CreatedAt,
                Status = x.Status,
                Total = x.Total
            })
            .ToListAsync(cancellationToken);
    }
}