using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Settings;
using BeautyCommerce.Infrastructure.Identity;
using BeautyCommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BeautyCommerce.Tests.Integrity;

public class LoginLockoutReproductionTests
{
    [Fact]
    public async Task Repeated_Failed_Logins_Never_Trigger_Lockout_And_The_Correct_Password_Still_Authenticates()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        using var _ = connection;
        connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddScoped(_ => Mock.Of<ICurrentUserService>());
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connection));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string email = "qa-lockout@test.local";
        const string correctPassword = "Correct123";
        const string wrongPassword = "WrongPass9";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "QA",
            LastName = "Lockout",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, correctPassword);
        createResult.Succeeded.Should().BeTrue(
            string.Join(";", createResult.Errors.Select(e => e.Description)));

        var jwtOptions = Options.Create(new JwtSettings
        {
            Key = "qa-only-throwaway-signing-key-not-used-in-production-0123456789",
            Issuer = "QaIssuer",
            Audience = "QaAudience",
            DurationInMinutes = 120
        });

        var identityService = new IdentityService(
            userManager,
            new JwtTokenGenerator(jwtOptions),
            NullLogger<IdentityService>.Instance);

        var before = await userManager.FindByEmailAsync(email);
        before!.AccessFailedCount.Should().Be(0);
        before.LockoutEnd.Should().BeNull();
        (await userManager.IsLockedOutAsync(before)).Should().BeFalse();

        const int attempts = 10;
        var failuresObserved = 0;

        for (var i = 0; i < attempts; i++)
        {
            try
            {
                await identityService.LoginAsync(email, wrongPassword);
            }
            catch (UnauthorizedException)
            {
                failuresObserved++;
            }
        }

        failuresObserved.Should().Be(attempts,
            "every one of the 10 wrong-password attempts must be rejected on its own merit (wrong password), not by a lockout kicking in early");

        var after = await userManager.FindByEmailAsync(email);
        after!.AccessFailedCount.Should().Be(0,
            "CheckPasswordAsync alone never increments AccessFailedCount — only PasswordSignInAsync/AccessFailedAsync do, and neither is ever called");
        after.LockoutEnd.Should().BeNull(
            "with AccessFailedCount never incremented, LockoutEnd is never set");
        (await userManager.IsLockedOutAsync(after)).Should().BeFalse(
            "no lockout is ever engaged, regardless of how many wrong passwords were tried");

        var successfulLogin = await identityService.LoginAsync(email, correctPassword);
        successfulLogin.Should().NotBeNull();
        successfulLogin.Token.Should().NotBeNullOrEmpty(
            "the correct password must still authenticate immediately after 10 failed attempts, with no delay or extra friction");
        successfulLogin.Email.Should().Be(email);
    }
}
