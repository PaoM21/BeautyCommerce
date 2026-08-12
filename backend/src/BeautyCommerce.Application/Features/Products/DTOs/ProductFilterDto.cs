using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautyCommerce.Application.Features.Products.DTOs
{
    public class ProductFilterDto
    {
        public string? Search { get; set; }

        public Guid? BrandId { get; set; }

        public Guid? CategoryId { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public string? Sort { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 12;
    }
}
