using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautyCommerce.Application.Features.ShoppingCart.Commands.RemoveItem
{
    public class RemoveCartItemCommand : IRequest
    {
        public Guid ProductVariantId { get; set; }
    }
}
