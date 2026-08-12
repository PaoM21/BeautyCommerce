namespace BeautyCommerce.Application.Common.Interfaces;

public interface ILoyaltyService
{
    Task AwardPointsForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken);
}
