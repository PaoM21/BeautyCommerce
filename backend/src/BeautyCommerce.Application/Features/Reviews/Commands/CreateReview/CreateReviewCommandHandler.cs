using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler
    : IRequestHandler<CreateReviewCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public CreateReviewCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ICacheService cache)
    {
        _context = context;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<Guid> Handle(
        CreateReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            throw new UnauthorizedAccessException();

        var userId = _currentUser.UserId.Value;

        if (request.Review.Rating < 1 ||
            request.Review.Rating > 5)
        {
            throw new BadRequestException(
                "La calificación debe estar entre 1 y 5.");
        }

        var productExists =
            await _context.Products
                .AnyAsync(
                    x => x.Id == request.Review.ProductId,
                    cancellationToken);

        if (!productExists)
            throw new NotFoundException(
                "Producto no encontrado.");
                
        var orderItem = await _context.OrderItems
            .Include(x => x.Order)
            .Include(x => x.ProductVariant)
            .Where(x =>
                x.Order.UserId == userId &&
                x.Order.Status == OrderStatus.Delivered &&
                x.ProductVariant.ProductId ==
                    request.Review.ProductId)
            .FirstOrDefaultAsync(
                cancellationToken);

        if (orderItem == null)
        {
            throw new BadRequestException(
                "Solo puedes reseñar productos que hayas comprado y recibido.");
        }

        var alreadyReviewed =
            await _context.Reviews.AnyAsync(
                x =>
                    x.UserId == userId &&
                    x.ProductId == request.Review.ProductId &&
                    x.Order != null &&
                    x.Order.Id == orderItem.OrderId,
                cancellationToken);

        if (alreadyReviewed)
        {
            throw new BadRequestException(
                "Ya has reseñado este producto.");
        }

        var review = new Review
        {
            UserId = userId,
            ProductId = request.Review.ProductId,
            Order = orderItem.Order,
            Rating = request.Review.Rating,
            Comment = request.Review.Comment.Trim()
        };

        _context.Reviews.Add(review);

        await _context.SaveChangesAsync(
            cancellationToken);
            
        await _cache.InvalidateTagAsync("Reviews");
        
        await _cache.InvalidateTagAsync("Products");

        return review.Id;
    }
}
