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

    public CreateProductCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
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
            product.Variants.Add(new ProductVariant
            {
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

        return product.Id;
    }
}