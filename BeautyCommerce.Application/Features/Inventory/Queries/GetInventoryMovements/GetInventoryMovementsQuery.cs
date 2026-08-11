using BeautyCommerce.Application.Features.Inventory.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Inventory.Queries.GetInventoryMovements;

public class GetInventoryMovementsQuery : IRequest<List<InventoryMovementDto>>
{
}
