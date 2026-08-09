using BeautyCommerce.Application.Features.Reviews.DTOs;
using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Reviews.Queries.GetProductReviews;

public class GetProductReviewsQueryHandler
    : IRequestHandler<
        GetProductReviewsQuery,
        List<ReviewDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductReviewsQueryHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReviewDto>> Handle(
        GetProductReviewsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Reviews
            .Where(x =>
                x.ProductId == request.ProductId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReviewDto
            {
                Id = x.Id,
                UserName = x.User.FirstName,
                Rating = x.Rating,
                Comment = x.Comment,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
