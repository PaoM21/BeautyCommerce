using BeautyCommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Orders.Queries.GetAdminOrderById;

public class GetAdminOrderByIdQuery : IRequest<OrderDto?>
{
    public Guid Id { get; set; }
}
