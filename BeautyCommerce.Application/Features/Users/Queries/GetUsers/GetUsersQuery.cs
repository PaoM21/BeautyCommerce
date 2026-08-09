using BeautyCommerce.Application.Features.Users.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Users.Queries.GetUsers;

public class GetUsersQuery : IRequest<List<UserDto>>
{
}
