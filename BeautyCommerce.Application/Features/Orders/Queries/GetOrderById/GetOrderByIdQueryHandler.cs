using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler
    : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetOrderByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<OrderDto?> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            throw new UnauthorizedAccessException();

        var userId = _currentUser.UserId.Value;

        return await _context.Orders
            .AsNoTracking()
            .Where(x =>
                x.Id == request.Id &&
                x.UserId == userId)
            .Select(x => new OrderDto
            {
                Id = x.Id,

                UserId = x.UserId,

                OrderNumber = x.OrderNumber,

                OrderDate = x.OrderDate,

                Status = x.Status.ToString(),

                SubTotal = x.SubTotal,

                ShippingCost = x.ShippingCost,

                Tax = x.Tax,

                Total = x.Total,

                TransactionId = x.TransactionId,

                Items = x.Items.Select(item => new OrderItemDto
                {
                    Id = item.Id,

                    ProductVariantId =
                        item.ProductVariantId,

                    ProductName =
                        item.ProductVariant.Product.Name,

                    Color =
                        item.ProductVariant.Color,

                    Size =
                        item.ProductVariant.Size,

                    Quantity =
                        item.Quantity,

                    UnitPrice =
                        item.UnitPrice,

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