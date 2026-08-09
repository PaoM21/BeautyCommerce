using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.ShoppingCart.Commands.RemoveItem
{
    public class RemoveCartItemCommandHandler
    : IRequestHandler<RemoveCartItemCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public RemoveCartItemCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            RemoveCartItemCommand request,
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
                throw new Exception("Carrito no encontrado.");

            var item = cart.Items.FirstOrDefault(x =>
                x.ProductVariantId == request.ProductVariantId);

            if (item == null)
                throw new Exception("Producto no existe en el carrito.");

            _context.ShoppingCartItems.Remove(item);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
