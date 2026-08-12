using BeautyCommerce.Application.Features.Inventory.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Inventory.Queries.GetInventory;

public class GetInventoryQuery : IRequest<List<InventoryItemDto>>
{
}
