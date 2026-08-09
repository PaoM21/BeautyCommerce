using BeautyCommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Orders.Queries.GetMyOrders;

public class GetMyOrdersQuery : IRequest<List<OrderDto>>
{
}
