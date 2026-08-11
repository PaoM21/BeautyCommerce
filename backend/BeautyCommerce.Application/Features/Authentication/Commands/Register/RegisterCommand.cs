using BeautyCommerce.Application.Features.Authentication.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Authentication.Commands.Register;

public class RegisterCommand : IRequest<Guid>
{
    public RegisterRequestDto User { get; set; } = new();
}