using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Infrastructure.Services;

public class LoyaltyService : ILoyaltyService
{
    private readonly IApplicationDbContext _context;

    public LoyaltyService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AwardPointsForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(
                x => x.Id == orderId,
                cancellationToken);

        if (order == null)
            return;

        // Evitar entregar puntos dos veces
        var alreadyAwarded =
            await _context.LoyaltyTransactions
                .AnyAsync(
                    x =>
                        x.OrderId == orderId &&
                        x.Type == "Purchase",
                    cancellationToken);

        if (alreadyAwarded)
            return;

        var points = (int)Math.Floor(
            order.Total / 1000m);

        if (points <= 0)
            return;

        var account =
            await _context.LoyaltyAccounts
                .FirstOrDefaultAsync(
                    x => x.UserId == order.UserId,
                    cancellationToken);

        if (account == null)
        {
            account = new LoyaltyAccount
            {
                UserId = order.UserId,
                Points = 0,
                Level = "Bronze"
            };

            _context.LoyaltyAccounts.Add(account);

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        account.Points += points;

        account.Level = CalculateLevel(
            account.Points);

        var transaction = new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            Points = points,
            Type = "Purchase",
            Description =
                $"Puntos por pedido {order.OrderNumber}",
            OrderId = order.Id
        };

        _context.LoyaltyTransactions.Add(
            transaction);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    private static string CalculateLevel(
        int points)
    {
        if (points >= 1500)
            return "Gold";

        if (points >= 500)
            return "Silver";

        return "Bronze";
    }
}
