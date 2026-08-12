using BeautyCommerce.Application.Features.Loyalty.Queries.GetMyLoyalty;
using BeautyCommerce.Application.Features.Loyalty.Queries.GetLoyaltyHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LoyaltyController : ControllerBase
{
    private readonly IMediator _mediator;

    public LoyaltyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyLoyalty(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMyLoyaltyQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetLoyaltyHistoryQuery(),
            cancellationToken);

        return Ok(result);
    }
}
