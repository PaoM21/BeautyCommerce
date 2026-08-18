using BeautyCommerce.Application.Common.Behaviors;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.ForgotPassword;

public class ForgotPasswordCommand : IRequest<Unit>, INotTransactional
{
    public string Email { get; set; } = string.Empty;
}
