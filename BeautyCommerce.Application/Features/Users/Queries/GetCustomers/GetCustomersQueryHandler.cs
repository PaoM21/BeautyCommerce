using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Users.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Users.Queries.GetCustomers;

public class GetCustomersQueryHandler
    : IRequestHandler<GetCustomersQuery, List<UserDto>>
{
    private readonly IUserService _userService;

    public GetCustomersQueryHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<List<UserDto>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        return await _userService.GetCustomersAsync(cancellationToken);
    }
}
