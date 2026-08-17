using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Features.Authentication.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.Login;

// Not transactional: a failed login must persist ASP.NET Identity's own
// AccessFailedCount/LockoutEnd bookkeeping even though LoginAsync throws
// UnauthorizedException, which TransactionBehavior otherwise rolls back.
public class LoginCommand : IRequest<LoginResponseDto>, INotTransactional
{
    public LoginRequestDto Login { get; set; } = new();
}