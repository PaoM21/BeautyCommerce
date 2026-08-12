using BeautyCommerce.Application.Common.Constants;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Users.DTOs;
using BeautyCommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<int> GetTotalCustomersAsync()
    {
        var customers = await _userManager.GetUsersInRoleAsync(Roles.Customer);

        return customers.Count;
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync(cancellationToken);

        return await MapUsersAsync(users);
    }

    public async Task<List<UserDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _userManager.GetUsersInRoleAsync(Roles.Customer);

        var users = customers
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToList();

        return await MapUsersAsync(users);
    }

    public async Task<Dictionary<Guid, UserDto>> GetUsersByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idSet = ids.Distinct().ToList();

        var users = await _userManager.Users
            .Where(x => idSet.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            x => x.Id,
            x => new UserDto
            {
                Id = x.Id,

                FullName = string.IsNullOrWhiteSpace(x.FirstName) && string.IsNullOrWhiteSpace(x.LastName)
                    ? x.UserName ?? string.Empty
                    : $"{x.FirstName} {x.LastName}".Trim(),

                Email = x.Email ?? string.Empty
            });
    }

    private async Task<List<UserDto>> MapUsersAsync(List<ApplicationUser> users)
    {
        var result = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new UserDto
            {
                Id = user.Id,

                FullName = string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
                    ? user.UserName ?? string.Empty
                    : $"{user.FirstName} {user.LastName}".Trim(),

                Email = user.Email ?? string.Empty,

                Role = roles.FirstOrDefault() ?? string.Empty,

                IsActive = user.IsActive
            });
        }

        return result;
    }
}
