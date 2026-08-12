using BeautyCommerce.Application.Features.Loyalty.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Loyalty.Queries.GetLoyaltyHistory;

public class GetLoyaltyHistoryQuery
    : IRequest<List<LoyaltyTransactionDto>>
{
}
