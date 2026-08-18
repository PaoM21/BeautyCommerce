using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Authentication.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BeautyCommerce.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<Guid> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password)
    {
        _logger?.LogInformation("Register attempt for {Email}", email);

        var exists = await _userManager.FindByEmailAsync(email);

        if (exists != null)
        {
            _logger?.LogWarning("Register failed: email already exists {Email}", email);
            throw new ConflictException("El correo ya está registrado.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        IdentityResult result;

        try
        {
            result = await _userManager.CreateAsync(user, password);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException pgEx &&
            pgEx.SqlState == PostgresErrorCodes.UniqueViolation &&
            (pgEx.ConstraintName == "UserNameIndex" || pgEx.ConstraintName == "EmailIndex"))
        {
            _logger?.LogWarning(
                "Register failed: concurrent request already registered {Email}", email);
            throw new ConflictException("El correo ya está registrado.");
        }

        if (!result.Succeeded)
        {
            var isDuplicate = result.Errors.Any(x =>
                x.Code == nameof(IdentityErrorDescriber.DuplicateUserName) ||
                x.Code == nameof(IdentityErrorDescriber.DuplicateEmail));

            if (isDuplicate)
            {
                _logger?.LogWarning(
                    "Register failed: concurrent request already registered {Email}", email);
                throw new ConflictException("El correo ya está registrado.");
            }

            var errors = string.Join(", ", result.Errors.Select(x => x.Description));
            _logger?.LogError("Register failed for {Email}: {Errors}", email, errors);
            throw new BadRequestException(errors);
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        _logger?.LogInformation("User registered successfully: {UserId} ({Email})", user.Id, email);

        return user.Id;
    }

    public async Task<LoginResponseDto> LoginAsync(
        string email,
        string password)
    {
        _logger?.LogInformation("Login attempt for {Email}", email);

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            _logger?.LogWarning("Login failed: user not found {Email}", email);
            throw new UnauthorizedException("Correo o contraseña incorrectos.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {

            _logger?.LogWarning(
                "Login failed for {Email}: {Reason}",
                email,
                result.IsLockedOut ? "locked out" : "invalid credentials");
            throw new UnauthorizedException("Correo o contraseña incorrectos.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var token = await _jwtTokenGenerator.GenerateTokenAsync(
            user.Id,
            user.Email!,
            $"{user.FirstName} {user.LastName}",
            roles);

        _logger?.LogInformation("Login successful for {UserId} ({Email})", user.Id, email);

        return token;
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            _logger?.LogInformation(
                "Password reset requested for unknown email {Email}", email);

            return null;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        _logger?.LogInformation(
            "Password reset token generated for {UserId} ({Email})", user.Id, email);

        return token;
    }

    public async Task<bool> ResetPasswordAsync(
        string email,
        string token,
        string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(x => x.Description));

            _logger?.LogWarning(
                "Password reset failed for {Email}: {Errors}", email, errors);

            return false;
        }

        _logger?.LogInformation("Password reset successfully for {Email}", email);

        return true;
    }
}