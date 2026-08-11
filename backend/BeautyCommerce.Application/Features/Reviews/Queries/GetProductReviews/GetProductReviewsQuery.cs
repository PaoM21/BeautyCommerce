using BeautyCommerce.Application.Features.Reviews.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Reviews.Queries.GetProductReviews;

public class GetProductReviewsQuery
    : IRequest<List<ReviewDto>>
{
    public Guid ProductId { get; set; }
}
