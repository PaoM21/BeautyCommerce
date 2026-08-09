namespace BeautyCommerce.Application.Common.Interfaces;

public interface IInventoryService
{
    Task RegisterEntryAsync(
        Guid productVariantId,
        int quantity,
        string reason,
        Guid? userId,
        CancellationToken cancellationToken);

    Task RegisterExitAsync(
        Guid productVariantId,
        int quantity,
        string reason,
        Guid? userId,
        CancellationToken cancellationToken);
}