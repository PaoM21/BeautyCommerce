using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Authentication.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BeautyCommerce.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
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

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
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

        var valid = await _userManager.CheckPasswordAsync(user, password);

        if (!valid)
        {
            _logger?.LogWarning("Login failed: invalid credentials for {Email}", email);
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
}