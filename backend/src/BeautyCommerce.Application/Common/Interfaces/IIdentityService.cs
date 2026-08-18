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

    /// <summary>
    /// Genera un token de reseteo de contraseña para el usuario con ese
    /// correo. Devuelve null si el usuario no existe — el llamador NO
    /// debe revelar esto al cliente (evita enumeración de usuarios).
    /// </summary>
    Task<string?> GeneratePasswordResetTokenAsync(string email);

    Task<bool> ResetPasswordAsync(
        string email,
        string token,
        string newPassword);
}