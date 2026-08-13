using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.ShoppingCart.Commands.UpdateItem
{
    public class UpdateCartItemCommandHandler
    : IRequestHandler<UpdateCartItemCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateCartItemCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdateCartItemCommand request,
            CancellationToken cancellationToken)
        {
            if (_currentUser.UserId == null)
                throw new UnauthorizedAccessException();

            var cart = await _context.ShoppingCarts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(
                    x => x.UserId == _currentUser.UserId.Value,
                    cancellationToken);

            if (cart == null)
                throw new NotFoundException("Carrito no encontrado.");

            var item = cart.Items.FirstOrDefault(x =>
                x.ProductVariantId == request.Item.ProductVariantId);

            if (item == null)
                throw new NotFoundException("Producto no encontrado en el carrito.");

            var variant = await _context.ProductVariants
                .FirstAsync(
                    x => x.Id == request.Item.ProductVariantId,
                    cancellationToken);

            if (variant.Stock < request.Item.Quantity)
                throw new BadRequestException("No hay stock suficiente.");

            item.Quantity = request.Item.Quantity;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
