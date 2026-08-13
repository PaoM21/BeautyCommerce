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

    /// <summary>
    /// Same atomic exit as <see cref="RegisterExitAsync"/>, but for callers
    /// (like checkout) that need to treat "not enough stock" as a normal
    /// outcome to branch on rather than an exception to catch. Returns
    /// false instead of throwing when stock is insufficient; still throws
    /// KeyNotFoundException for a nonexistent variant and ArgumentException
    /// for an invalid quantity, since those are caller bugs, not a business
    /// outcome.
    /// </summary>
    Task<bool> TryRegisterExitAsync(
        Guid productVariantId,
        int quantity,
        string reason,
        Guid? userId,
        CancellationToken cancellationToken);
}