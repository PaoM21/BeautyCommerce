using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautyCommerce.Application.Features.ShoppingCart.Commands.Clear
{
    public class ClearCartCommandHandler
    : IRequestHandler<ClearCartCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ClearCartCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            ClearCartCommand request,
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
                return;

            _context.ShoppingCartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
