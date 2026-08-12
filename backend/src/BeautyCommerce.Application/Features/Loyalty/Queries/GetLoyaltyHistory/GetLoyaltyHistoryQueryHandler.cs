using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Loyalty.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Loyalty.Queries.GetLoyaltyHistory;

public class GetLoyaltyHistoryQueryHandler
    : IRequestHandler<
        GetLoyaltyHistoryQuery,
        List<LoyaltyTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetLoyaltyHistoryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<LoyaltyTransactionDto>> Handle(
        GetLoyaltyHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            throw new UnauthorizedAccessException();

        var userId = _currentUser.UserId.Value;

        return await _context.LoyaltyTransactions
            .Where(x =>
                x.LoyaltyAccount.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LoyaltyTransactionDto
            {
                Id = x.Id,
                Points = x.Points,
                Type = x.Type,
                Description = x.Description,
                OrderId = x.OrderId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
