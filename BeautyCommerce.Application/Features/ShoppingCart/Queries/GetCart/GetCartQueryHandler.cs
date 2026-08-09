using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.ShoppingCart.Queries.GetCart;

public class GetCartQueryHandler
    : IRequestHandler<GetCartQuery, ShoppingCartDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCartQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ShoppingCartDto> Handle(
        GetCartQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            throw new UnauthorizedAccessException();

        var cart = await _context.ShoppingCarts
            .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(
                x => x.UserId == _currentUser.UserId.Value,
                cancellationToken);

        if (cart == null)
            return new ShoppingCartDto();

        var dto = new ShoppingCartDto();

        foreach (var item in cart.Items)
        {
            dto.Items.Add(new ShoppingCartItemDto
            {
                ProductId = item.ProductVariant.ProductId,
                ProductVariantId = item.ProductVariantId,
                ProductName = item.ProductVariant.Product.Name,
                Color = item.ProductVariant.Color,
                Size = item.ProductVariant.Size,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Quantity * item.UnitPrice,
                ImageUrl = item.ProductVariant.Product.Images
                    .FirstOrDefault()?.ImageUrl
            });
        }

        dto.Total = dto.Items.Sum(x => x.Subtotal);

        return dto;
    }
}
