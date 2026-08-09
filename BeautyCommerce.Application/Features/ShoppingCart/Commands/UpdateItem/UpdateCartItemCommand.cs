using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautyCommerce.Application.Features.ShoppingCart.Commands.UpdateItem
{
    public class UpdateCartItemCommand : IRequest
    {
        public UpdateCartItemDto Item { get; set; } = new();
    }
}
