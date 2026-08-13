using AutoMapper;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;
    private readonly IProductVariantIdentifierGenerator _identifierGenerator;

    public CreateProductCommandHandler(
        IApplicationDbContext context,
        ICacheService cache,
        IProductVariantIdentifierGenerator identifierGenerator)
    {
        _context = context;
        _cache = cache;
        _identifierGenerator = identifierGenerator;
    }
    public async Task<Guid> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            IsFeatured = request.IsFeatured,
        };

        foreach (var variant in request.Variants)
        {
            // SKU/Barcode are technical inventory identifiers, not
            // something the client provides — the backend generates and
            // guarantees their uniqueness (see IProductVariantIdentifierGenerator).
            product.Variants.Add(new ProductVariant
            {
                SKU = await _identifierGenerator.GenerateSkuAsync(cancellationToken),
                Barcode = await _identifierGenerator.GenerateBarcodeAsync(cancellationToken),
                Price = variant.Price,
                Stock = variant.Stock
            });
        }

        foreach (var image in request.Images)
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl = image
            });
        }

        await _context.Products.AddAsync(product, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateTagAsync("Products");

        return product.Id;
    }
}