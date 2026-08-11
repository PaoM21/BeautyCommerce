using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler
    : IRequestHandler<UpdateProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public UpdateProductCommandHandler(
        IApplicationDbContext context,
        ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (product == null)
            throw new Exception("Producto no encontrado.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.BrandId = request.BrandId;
        product.CategoryId = request.CategoryId;
        product.IsFeatured = request.IsFeatured;

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateTagAsync("Products");
    }
}