using BeautyCommerce.Application.Common.Helpers;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Products.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Products.Queries.GetProductById;

public class GetProductByIdHandler
    : IRequestHandler<GetProductByIdQuery, ProductDetailDto>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDetailDto> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetWithDetailsAsync(request.Id);

        Guard.AgainstNull(product, "Producto no encontrado.");

        return new ProductDetailDto
        {
            Id = product.Id,

            Name = product.Name,

            Description = product.Description,

            BrandId = product.BrandId,

            CategoryId = product.CategoryId,

            Brand = product.Brand?.Name ?? "Sin marca",

            Category = product.Category?.Name ?? "Sin categoría",

            IsFeatured = product.IsFeatured,

            Variants = product.Variants
                .Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    SKU = v.SKU,
                    Price = v.Price,
                    Stock = v.Stock,
                    Color = v.Color,
                    Size = v.Size
                })
                .ToList(),

            Images = product.Images
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    Url = i.ImageUrl
                })
                .ToList()
        };
    }
}