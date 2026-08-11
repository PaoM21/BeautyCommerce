using BeautyCommerce.Application.Common.Constants;
using BeautyCommerce.Application.Features.Users.Queries.GetCustomers;
using BeautyCommerce.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var users = await _mediator.Send(new GetUsersQuery());

        return Ok(users);
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers()
    {
        var customers = await _mediator.Send(new GetCustomersQuery());

        return Ok(customers);
    }
}
