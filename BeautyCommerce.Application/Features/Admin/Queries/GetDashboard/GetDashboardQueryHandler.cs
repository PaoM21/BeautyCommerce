using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Admin.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Admin.Queries.GetDashboard;

public class GetDashboardQueryHandler
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var totalSales = await _context.Orders
            .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0;

        var totalOrders = await _context.Orders
            .CountAsync(cancellationToken);

        var totalProducts = await _context.Products
            .CountAsync(cancellationToken);

        var totalCustomers = 0;

        var lowStockProducts = await _context.ProductVariants
            .CountAsync(x => x.Stock > 0 && x.Stock <= 5,
                cancellationToken);

        var outOfStockProducts = await _context.ProductVariants
            .CountAsync(x => x.Stock == 0,
                cancellationToken);

        var lastOrders = await _context.Orders
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new DashboardOrderDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                Customer = x.UserId.ToString(),
                Total = x.Total,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var salesByMonth = await _context.Orders
            .GroupBy(x => new
            {
                x.CreatedAt.Year,
                x.CreatedAt.Month
            })
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .Select(x => new MonthlySalesDto
            {
                Month = $"{x.Key.Month}/{x.Key.Year}",
                Total = x.Sum(o => o.Total)
            })
            .ToListAsync(cancellationToken);

        return new DashboardDto
        {
            TotalSales = totalSales,
            TotalOrders = totalOrders,
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts,
            //LowStockProducts = lowStockProducts,
            //OutOfStockProducts = outOfStockProducts,
            //LastOrders = lastOrders,
            //SalesByMonth = salesByMonth
        };
    }
}