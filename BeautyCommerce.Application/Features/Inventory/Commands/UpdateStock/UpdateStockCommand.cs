using MediatR;

namespace BeautyCommerce.Application.Features.Inventory.Commands.UpdateStock;

public class UpdateStockCommand : IRequest
{
    public Guid ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public bool IsEntry { get; set; }

    public string Reason { get; set; } = string.Empty;
}
