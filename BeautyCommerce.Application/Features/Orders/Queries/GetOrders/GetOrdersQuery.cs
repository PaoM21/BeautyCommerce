using BeautyCommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Orders.Queries.GetOrders;

public class GetOrdersQuery : IRequest<List<OrderSummaryDto>>
{
}