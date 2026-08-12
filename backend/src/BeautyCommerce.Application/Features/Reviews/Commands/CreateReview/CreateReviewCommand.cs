using BeautyCommerce.Application.Features.Reviews.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommand : IRequest<Guid>
{
    public CreateReviewDto Review { get; set; } = new();
}
