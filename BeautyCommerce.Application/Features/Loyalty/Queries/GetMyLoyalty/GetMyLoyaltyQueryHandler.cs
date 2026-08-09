using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Loyalty.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Loyalty.Queries.GetMyLoyalty;

public class GetMyLoyaltyQueryHandler
    : IRequestHandler<
        GetMyLoyaltyQuery,
        LoyaltyAccountDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyLoyaltyQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<LoyaltyAccountDto> Handle(
        GetMyLoyaltyQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            throw new UnauthorizedAccessException();

        var account =
            await _context.LoyaltyAccounts
                .FirstOrDefaultAsync(
                    x => x.UserId ==
                        _currentUser.UserId.Value,
                    cancellationToken);

        if (account == null)
        {
            return new LoyaltyAccountDto
            {
                Points = 0,
                Level = "Bronze"
            };
        }

        return new LoyaltyAccountDto
        {
            Points = account.Points,
            Level = account.Level
        };
    }
}
