using BeautyCommerce.Application.Features.Users.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Users.Queries.GetCustomers;

public class GetCustomersQuery : IRequest<List<UserDto>>
{
}
