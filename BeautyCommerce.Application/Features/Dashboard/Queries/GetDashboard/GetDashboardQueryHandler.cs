using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Dashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BeautyCommerce.Domain.Enums;

namespace BeautyCommerce.Application.Features.Dashboard.Queries.GetDashboard;

public class GetDashboardQueryHandler
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetDashboardQueryHandler(
        IApplicationDbContext context,
        IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<DashboardDto> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var firstDay = new DateTime(
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month,
            1);

        var dashboard = new DashboardDto
        {
            TotalProducts = await _context.Products.CountAsync(cancellationToken),

            TotalOrders = await _context.Orders.CountAsync(cancellationToken),

            TotalCustomers = await _userService.GetTotalCustomersAsync(),

            TotalSales = await _context.Orders
                .Where(x => x.Status == OrderStatus.Delivered)
                .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0,

            SalesThisMonth = await _context.Orders
                .Where(x =>
                    x.Status == OrderStatus.Delivered &&
                    x.OrderDate >= firstDay)
                .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0,

            PendingOrders = await _context.Orders
                .CountAsync(
                    x => x.Status == OrderStatus.Pending,
                    cancellationToken)
        };

        return dashboard;
    }
}
