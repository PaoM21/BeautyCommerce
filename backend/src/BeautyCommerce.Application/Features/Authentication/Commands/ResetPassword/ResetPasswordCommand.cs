using BeautyCommerce.Application.Common.Behaviors;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.ResetPassword;

public class ResetPasswordCommand : IRequest<Unit>, INotTransactional
{
    public string Email { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
