using BeautyCommerce.Application.Features.Reviews.DTOs;
using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Reviews.Queries.GetFeaturedReviews;

public class GetFeaturedReviewsQueryHandler
    : IRequestHandler<
        GetFeaturedReviewsQuery,
        List<FeaturedReviewDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetFeaturedReviewsQueryHandler(
        IApplicationDbContext context,
        IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<List<FeaturedReviewDto>> Handle(
        GetFeaturedReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var reviews = await _context.Reviews
            .Where(x => x.Rating >= 4 && !string.IsNullOrWhiteSpace(x.Comment))
            .OrderByDescending(x => x.Rating)
            .ThenByDescending(x => x.CreatedAt)
            .Take(request.Count)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.Rating,
                x.Comment,
                x.CreatedAt,
                x.ProductId,
                ProductName = x.Product.Name
            })
            .ToListAsync(cancellationToken);

        var users = await _userService.GetUsersByIdsAsync(
            reviews.Select(x => x.UserId),
            cancellationToken);

        return reviews
            .Select(x => new FeaturedReviewDto
            {
                Id = x.Id,
                UserName = users.TryGetValue(x.UserId, out var user)
                    ? user.FullName
                    : "Cliente verificado",
                Rating = x.Rating,
                Comment = x.Comment,
                CreatedAt = x.CreatedAt,
                ProductId = x.ProductId,
                ProductName = x.ProductName
            })
            .ToList();
    }
}
