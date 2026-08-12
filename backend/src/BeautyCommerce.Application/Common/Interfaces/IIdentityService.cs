using BeautyCommerce.Application.Features.Authentication.DTOs;

namespace BeautyCommerce.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Guid> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password);

    Task<LoginResponseDto> LoginAsync(
    string email,
    string password);
}