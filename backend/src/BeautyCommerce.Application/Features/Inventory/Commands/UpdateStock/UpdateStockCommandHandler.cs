using BeautyCommerce.Application.Common.Interfaces;
using MediatR;

namespace BeautyCommerce.Application.Features.Inventory.Commands.UpdateStock;

public class UpdateStockCommandHandler : IRequestHandler<UpdateStockCommand>
{
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateStockCommandHandler(
        IInventoryService inventoryService,
        ICurrentUserService currentUserService)
    {
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        UpdateStockCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("El motivo es obligatorio.");
        }

        var userId = _currentUserService.UserId;

        if (request.IsEntry)
        {
            await _inventoryService.RegisterEntryAsync(
                request.ProductVariantId,
                request.Quantity,
                request.Reason,
                userId,
                cancellationToken);

            return;
        }

        await _inventoryService.RegisterExitAsync(
            request.ProductVariantId,
            request.Quantity,
            request.Reason,
            userId,
            cancellationToken);
    }
}
