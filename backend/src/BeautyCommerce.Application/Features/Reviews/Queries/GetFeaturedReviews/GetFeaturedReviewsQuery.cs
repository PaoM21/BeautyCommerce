using BeautyCommerce.Application.Features.Reviews.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Reviews.Queries.GetFeaturedReviews;

public class GetFeaturedReviewsQuery
    : IRequest<List<FeaturedReviewDto>>
{
    public int Count { get; set; } = 6;
}
