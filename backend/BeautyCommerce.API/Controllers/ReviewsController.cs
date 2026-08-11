using BeautyCommerce.Application.Features.Reviews.Commands.CreateReview;
using BeautyCommerce.Application.Features.Reviews.Queries.GetProductReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            Success = true,
            ReviewId = id,
            Message = "Reseña creada correctamente."
        });
    }

    [AllowAnonymous]
    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetProductReviewsQuery
            {
                ProductId = productId
            },
            cancellationToken);

        return Ok(result);
    }
}
