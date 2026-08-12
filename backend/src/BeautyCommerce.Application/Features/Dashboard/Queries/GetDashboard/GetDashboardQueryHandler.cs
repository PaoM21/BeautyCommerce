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
        var now = DateTime.UtcNow;

        var firstDay = new DateTime(
            now.Year,
            now.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var dashboard = new DashboardDto
        {
            TotalProducts = await _context.Products.CountAsync(cancellationToken),

            TotalOrders = await _context.Orders.CountAsync(cancellationToken),

            TotalCustomers = await _userService.GetTotalCustomersAsync(),

            TotalSales = await _context.Orders
                .Where(x => x.Status != OrderStatus.Cancelled)
                .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0,

            SalesThisMonth = await _context.Orders
                .Where(x =>
                    x.Status != OrderStatus.Cancelled &&
                    x.OrderDate >= firstDay)
                .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0,

            PendingOrders = await _context.Orders
                .CountAsync(
                    x =>
                        x.Status == OrderStatus.Pending ||
                        x.Status == OrderStatus.Paid ||
                        x.Status == OrderStatus.Processing,
                    cancellationToken),

            LowStockProducts = await _context.ProductVariants
                .CountAsync(
                    x => x.Stock > 0 && x.Stock <= x.MinimumStock,
                    cancellationToken),

            OutOfStockProducts = await _context.ProductVariants
                .CountAsync(
                    x => x.Stock <= 0,
                    cancellationToken),

            LastOrders = await _context.Orders
                .AsNoTracking()
                .OrderByDescending(x => x.OrderDate)
                .Take(10)
                .Select(x => new DashboardOrderDto
                {
                    Id = x.Id,
                    OrderNumber = x.OrderNumber,
                    Customer = x.UserId.ToString(),
                    Total = x.Total,
                    CreatedAt = x.OrderDate
                })
                .ToListAsync(cancellationToken),

            SalesByMonth = await _context.Orders
                .AsNoTracking()
                .Where(x => x.Status != OrderStatus.Cancelled)
                .GroupBy(x => new { x.OrderDate.Year, x.OrderDate.Month })
                .OrderBy(x => x.Key.Year)
                .ThenBy(x => x.Key.Month)
                .Select(x => new MonthlySalesDto
                {
                    Month = $"{x.Key.Month:D2}/{x.Key.Year}",
                    Total = x.Sum(o => o.Total)
                })
                .ToListAsync(cancellationToken)
        };

        return dashboard;
    }
}
