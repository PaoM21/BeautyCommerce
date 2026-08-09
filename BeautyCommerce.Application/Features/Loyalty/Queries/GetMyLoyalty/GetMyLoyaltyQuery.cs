using BeautyCommerce.Application.Features.Loyalty.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Loyalty.Queries.GetMyLoyalty;

public class GetMyLoyaltyQuery
    : IRequest<LoyaltyAccountDto>
{
}
