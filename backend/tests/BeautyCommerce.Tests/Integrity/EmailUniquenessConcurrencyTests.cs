using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Infrastructure.Identity;
using BeautyCommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;

namespace BeautyCommerce.Tests.Integrity;

[Trait("Category", "Integration")]
public class EmailUniquenessConcurrencyTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    private static (UserManager<ApplicationUser> UserManager, SignInManager<ApplicationUser> SignInManager) BuildIdentityManagers(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.AddSingleton(context);

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        return (
            provider.GetRequiredService<UserManager<ApplicationUser>>(),
            provider.GetRequiredService<SignInManager<ApplicationUser>>());
    }

    private async Task<(int successCount, List<ApplicationUser> usersWithThisEmail)> RaceTwoRegistrationsAsync(
        string emailA, string emailB, string queryEmailForCleanup)
    {
        async Task<bool> TryRegister(string firstName, string email)
        {
            await using var context = NewContext();
            var (userManager, signInManager) = BuildIdentityManagers(context);
            var identityService = new IdentityService(
                userManager, signInManager, Mock.Of<IJwtTokenGenerator>(), NullLogger<IdentityService>.Instance);

            try
            {
                await identityService.RegisterAsync(firstName, "Race", email, "Qa12345!");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        var taskA = TryRegister("A", emailA);
        var taskB = TryRegister("B", emailB);

        var results = await Task.WhenAll(taskA, taskB);
        var successCount = results.Count(x => x);

        await using var verify = NewContext();
        var usersWithThisEmail = await verify.Users
            .Where(u => u.NormalizedEmail == queryEmailForCleanup.ToUpperInvariant())
            .ToListAsync();

        return (successCount, usersWithThisEmail);
    }

    [Fact]
    public async Task Concurrent_Registrations_Loser_Receives_ConflictException_Not_A_Generic_Failure()
    {
        var email = $"qa-email-race-contract-{Guid.NewGuid():N}@test.local";

        async Task<(bool Succeeded, Exception? Error)> TryRegister(string firstName)
        {
            await using var context = NewContext();
            var (userManager, signInManager) = BuildIdentityManagers(context);
            var identityService = new IdentityService(
                userManager, signInManager, Mock.Of<IJwtTokenGenerator>(), NullLogger<IdentityService>.Instance);

            try
            {
                await identityService.RegisterAsync(firstName, "Race", email, "Qa12345!");
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
        }

        var taskA = TryRegister("A");
        var taskB = TryRegister("B");

        var results = await Task.WhenAll(taskA, taskB);

        try
        {
            results.Count(r => r.Succeeded).Should().Be(1, "exactly one registration should win the race");

            var loser = results.Single(r => !r.Succeeded);

            loser.Error.Should().NotBeNull("the losing request must not silently succeed");
            loser.Error.Should().BeOfType<ConflictException>(
                "the race loser must surface the same contractual error as a non-concurrent duplicate registration, not a generic/unhandled exception");
            loser.Error!.Message.Should().Be(
                "El correo ya está registrado.",
                "the message must match exactly what the non-concurrent duplicate-email pre-check already returns");

            await using var verify = NewContext();
            var usersWithThisEmail = await verify.Users
                .Where(u => u.NormalizedEmail == email.ToUpperInvariant())
                .ToListAsync();

            usersWithThisEmail.Should().HaveCount(1, "only one account should exist after the race settles");
        }
        finally
        {
            await using var cleanup = NewContext();
            var usersToRemove = await cleanup.Users
                .Where(u => u.NormalizedEmail == email.ToUpperInvariant())
                .ToListAsync();

            foreach (var user in usersToRemove)
                cleanup.Users.Attach(user).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;

            await cleanup.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Non_Duplicate_Validation_Failure_Still_Throws_BadRequestException()
    {
        await using var context = NewContext();
        var (userManager, signInManager) = BuildIdentityManagers(context);
        var identityService = new IdentityService(
            userManager, signInManager, Mock.Of<IJwtTokenGenerator>(), NullLogger<IdentityService>.Instance);

        var email = $"qa-weak-password-{Guid.NewGuid():N}@test.local";

        var act = () => identityService.RegisterAsync("QA", "WeakPassword", email, "weak");

        // A password that fails the configured strength rules must remain a
        // BadRequestException — the new DuplicateUserName/DuplicateEmail
        // carve-out must not swallow unrelated validation failures.
        await act.Should().ThrowAsync<BadRequestException>();

        try
        {
            (await act.Should().ThrowAsync<Exception>()).Which.Should().NotBeOfType<ConflictException>(
                "a weak password has nothing to do with a duplicate identifier");
        }
        finally
        {
            await using var cleanup = NewContext();
            var users = await cleanup.Users
                .Where(u => u.NormalizedEmail == email.ToUpperInvariant())
                .ToListAsync();

            foreach (var user in users)
                cleanup.Users.Attach(user).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;

            await cleanup.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Concurrent_Registrations_With_The_Same_Email()
    {
        var email = $"qa-email-race-{Guid.NewGuid():N}@test.local";

        var (successCount, usersWithThisEmail) = await RaceTwoRegistrationsAsync(email, email, email);

        try
        {
            successCount.Should().Be(1, "exactly one registration should win the race");

            usersWithThisEmail.Should().HaveCount(
                1,
                "only one account should exist for this email after the race settles");
        }
        finally
        {
            await using var cleanup = NewContext();
            foreach (var user in usersWithThisEmail)
                cleanup.Users.Attach(user).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
            await cleanup.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Concurrent_Registrations_With_The_Same_Email_Different_Case()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var emailLower = $"qa-email-case-{suffix}@test.local";
        var emailUpper = $"QA-EMAIL-CASE-{suffix}@TEST.LOCAL";

        var (successCount, usersWithThisEmail) = await RaceTwoRegistrationsAsync(emailLower, emailUpper, emailLower);

        try
        {
            successCount.Should().Be(
                1,
                "two emails differing only by case must be treated as the same account — exactly one registration should win");

            usersWithThisEmail.Should().HaveCount(1, "only one account should exist after the race settles");
        }
        finally
        {
            await using var cleanup = NewContext();
            foreach (var user in usersWithThisEmail)
                cleanup.Users.Attach(user).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
            await cleanup.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Database_Rejects_Duplicate_NormalizedEmail_Even_When_UserName_Differs()
    {
        var email = $"qa-email-raw-{Guid.NewGuid():N}@test.local";
        var normalizedEmail = email.ToUpperInvariant();

        await using var context = NewContext();

        var idA = Guid.NewGuid();
        var userNameA = $"qa-username-a-{idA:N}";
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "AspNetUsers"
                ("Id", "FirstName", "LastName", "IsActive", "UserName", "NormalizedUserName",
                 "Email", "NormalizedEmail", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled",
                 "LockoutEnabled", "AccessFailedCount")
            VALUES
                ({idA}, 'QA', 'RawA', true, {userNameA}, {userNameA.ToUpperInvariant()},
                 {email}, {normalizedEmail}, false, false, false, true, 0)
            """);

        try
        {
            var idB = Guid.NewGuid();
            var userNameB = $"qa-username-b-{idB:N}";

            var act = () => context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AspNetUsers"
                    ("Id", "FirstName", "LastName", "IsActive", "UserName", "NormalizedUserName",
                     "Email", "NormalizedEmail", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled",
                     "LockoutEnabled", "AccessFailedCount")
                VALUES
                    ({idB}, 'QA', 'RawB', true, {userNameB}, {userNameB.ToUpperInvariant()},
                     {email}, {normalizedEmail}, false, false, false, true, 0)
                """);

            var thrown = await act.Should().ThrowAsync<PostgresException>(
                "PostgreSQL must reject a second row with the same NormalizedEmail, regardless of UserName");

            thrown.Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            thrown.Which.ConstraintName.Should().Be("EmailIndex", "the rejection must come specifically from EmailIndex, not UserNameIndex");

            var count = await context.Users.AsNoTracking().CountAsync(u => u.NormalizedEmail == normalizedEmail);
            count.Should().Be(1, "the rejected second insert must not have created a row");
        }
        finally
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"AspNetUsers\" WHERE \"NormalizedEmail\" = {normalizedEmail}");
        }
    }
}
