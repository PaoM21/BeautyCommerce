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

// 6.4.2 finding #3: Program.cs sets options.User.RequireUniqueEmail = true,
// but the real unique index on AspNetUsers only covered NormalizedUserName
// (EmailIndex was non-unique) — confirmed by inspecting the schema directly
// in 6.4.1. RequireUniqueEmail only adds an application-level check inside
// UserManager.CreateAsync; it did not add a database constraint. A code
// audit (6.4.6-A.1) then confirmed IdentityService.RegisterAsync is the
// only place that ever creates a user, and it happens to set
// UserName = Email — but nothing enforces that as an invariant, so the old
// protection (piggybacking on UserNameIndex) was accidental, not designed.
//
// 6.4.6-A.3 closed that gap: migration 20260813043827_MakeEmailIndexUnique
// redefines the existing EmailIndex as UNIQUE on NormalizedEmail, instead
// of relying on UserName always mirroring Email. These tests cover both
// angles: Concurrent_Registrations_With_The_Same_Email and
// Concurrent_Registrations_With_The_Same_Email_Different_Case exercise the
// real IdentityService.RegisterAsync end-to-end; Database_Rejects_Duplicate_NormalizedEmail_Even_When_UserName_Differs
// bypasses the app layer entirely (like 6.4.4/6.4.5's raw-SQL tests) to
// prove EmailIndex itself is what rejects the duplicate — not
// UserNameIndex riding along for the ride.
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

    // Mirrors Program.cs's AddIdentity<ApplicationUser, ApplicationRole>(...)
    // configuration exactly, so the behavior under test is the real
    // production configuration, not a simplified stand-in.
    private static UserManager<ApplicationUser> BuildUserManager(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
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
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider().GetRequiredService<UserManager<ApplicationUser>>();
    }

    private async Task<(int successCount, List<ApplicationUser> usersWithThisEmail)> RaceTwoRegistrationsAsync(
        string emailA, string emailB, string queryEmailForCleanup)
    {
        async Task<bool> TryRegister(string firstName, string email)
        {
            await using var context = NewContext();
            var userManager = BuildUserManager(context);
            var identityService = new IdentityService(
                userManager, Mock.Of<IJwtTokenGenerator>(), NullLogger<IdentityService>.Instance);

            try
            {
                await identityService.RegisterAsync(firstName, "Race", email, "Qa12345!");
                return true;
            }
            catch (Exception)
            {
                // IdentityService.RegisterAsync throws (either a
                // ConflictException from its own pre-check, or a raw
                // exception wrapping the PostgreSQL unique_violation if
                // both requests got past the pre-check simultaneously) when
                // the email is already taken.
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
    public async Task Concurrent_Registrations_With_The_Same_Email()
    {
        var email = $"qa-email-race-{Guid.NewGuid():N}@test.local";

        var (successCount, usersWithThisEmail) = await RaceTwoRegistrationsAsync(email, email, email);

        try
        {
            // Now double-protected: UserNameIndex (UserName == Email at
            // registration) AND EmailIndex (NormalizedEmail, added in
            // 6.4.6-A.3) both independently reject the loser.
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
        // Uniqueness is enforced on NormalizedEmail (uppercase), so this
        // must be rejected exactly like an identical string would be —
        // "Test@Example.com" and "TEST@EXAMPLE.COM" are the same account.
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
        // Isolates EmailIndex from UserNameIndex: two rows with completely
        // different UserName/NormalizedUserName (so UserNameIndex has
        // nothing to say here) but the same NormalizedEmail. Bypasses
        // IdentityService/UserManager entirely via raw SQL, the same way
        // 6.4.4/6.4.5 proved their constraints directly. If this is
        // rejected, it can only be EmailIndex doing it.
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
