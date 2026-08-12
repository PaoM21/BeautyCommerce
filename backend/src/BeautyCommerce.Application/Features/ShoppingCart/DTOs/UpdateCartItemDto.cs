using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautyCommerce.Application.Features.ShoppingCart.DTOs
{
    public class UpdateCartItemDto
    {
        public Guid ProductVariantId { get; set; }

        public int Quantity { get; set; }
    }
}
