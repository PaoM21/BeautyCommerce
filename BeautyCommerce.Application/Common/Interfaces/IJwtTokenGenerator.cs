using BeautyCommerce.Application.Features.Authentication.DTOs;

namespace BeautyCommerce.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    Task<LoginResponseDto> GenerateTokenAsync(
        Guid userId,
        string email,
        string fullName,
        IList<string> roles);
}