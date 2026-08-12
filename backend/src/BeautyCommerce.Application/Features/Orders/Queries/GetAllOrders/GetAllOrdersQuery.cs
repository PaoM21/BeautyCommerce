using BeautyCommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Orders.Queries.GetAllOrders;

public class GetAllOrdersQuery : IRequest<List<OrderDto>>
{
}
