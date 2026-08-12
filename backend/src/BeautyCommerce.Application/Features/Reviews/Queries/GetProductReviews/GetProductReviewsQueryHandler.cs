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
    private readonly IUserService _userService;

    public GetProductReviewsQueryHandler(
        IApplicationDbContext context,
        IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<List<ReviewDto>> Handle(
        GetProductReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var reviews = await _context.Reviews
            .Where(x =>
                x.ProductId == request.ProductId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.Rating,
                x.Comment,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var users = await _userService.GetUsersByIdsAsync(
            reviews.Select(x => x.UserId),
            cancellationToken);

        return reviews
            .Select(x => new ReviewDto
            {
                Id = x.Id,
                UserName = users.TryGetValue(x.UserId, out var user)
                    ? user.FullName
                    : "Usuario",
                Rating = x.Rating,
                Comment = x.Comment,
                CreatedAt = x.CreatedAt
            })
            .ToList();
    }
}
