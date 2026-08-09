using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Users.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly IUserService _userService;

    public GetUsersQueryHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<List<UserDto>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        return await _userService.GetAllAsync(cancellationToken);
    }
}
