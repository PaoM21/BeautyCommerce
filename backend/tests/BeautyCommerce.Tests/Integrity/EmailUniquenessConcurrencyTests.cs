using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Infrastructure.Identity;
using BeautyCommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Integrity;

// 6.4.2 finding #3: Program.cs sets options.User.RequireUniqueEmail = true,
// but the real unique index on AspNetUsers only covers NormalizedUserName
// (EmailIndex is non-unique) — confirmed by inspecting the schema directly
// in 6.4.1. RequireUniqueEmail only adds an application-level check inside
// UserManager.CreateAsync; it does not add a database constraint. This
// test drives the real IdentityService.RegisterAsync (via a real
// UserManager<ApplicationUser>, configured exactly like Program.cs) twice,
// concurrently, with the same email, against real PostgreSQL, to see what
// actually happens — not what the option name implies should happen.
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

    [Fact]
    public async Task Concurrent_Registrations_With_The_Same_Email()
    {
        var email = $"qa-email-race-{Guid.NewGuid():N}@test.local";

        async Task<bool> TryRegister(string firstName)
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
                // IdentityService.RegisterAsync throws a plain Exception
                // (see 6.1) when UserManager reports the email/username is
                // already taken — either a genuine "duplicate" outcome, or
                // (if the DB truly has no constraint) both could avoid this
                // entirely and both succeed.
                return false;
            }
        }

        var taskA = TryRegister("A");
        var taskB = TryRegister("B");

        var results = await Task.WhenAll(taskA, taskB);

        var successCount = results.Count(x => x);

        await using var verify = NewContext();
        var usersWithThisEmail = await verify.Users
            .Where(u => u.Email == email)
            .ToListAsync();

        try
        {
            // Documents the real outcome. Per the audit: UserName is set
            // equal to Email at registration, and NormalizedUserName *is*
            // uniquely indexed — so today, one of the two concurrent
            // registrations is expected to fail, but only as a side effect
            // of the username collision, not because Email itself is
            // protected. If this assertion ever fails with successCount
            // == 2, it proves the accidental protection stopped working
            // (e.g. if UserName and Email are ever allowed to diverge).
            successCount.Should().Be(
                1,
                "exactly one registration should win — today this is only guaranteed by NormalizedUserName's " +
                "unique index, since UserName is always set equal to Email, not by any constraint on Email itself");

            usersWithThisEmail.Should().HaveCount(
                1,
                "only one account should exist for this email after the race settles");
        }
        finally
        {
            foreach (var user in usersWithThisEmail)
            {
                verify.Users.Remove(user);
            }

            await verify.SaveChangesAsync();
        }
    }
}
