using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautyCommerce.Application.Features.Products.DTOs
{
    public class ProductListItemDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string? Image { get; set; }
    }
}
