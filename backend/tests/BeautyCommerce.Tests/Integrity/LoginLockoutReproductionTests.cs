using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Settings;
using BeautyCommerce.Application.Features.Authentication.Commands.Login;
using BeautyCommerce.Application.Features.Authentication.DTOs;
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
    private const string Email = "qa-lockout@test.local";
    private const string CorrectPassword = "Correct123";
    private const string WrongPassword = "WrongPass9";

    private static async Task<(IdentityService IdentityService, UserManager<ApplicationUser> UserManager, ApplicationUser User, ApplicationDbContext Context)> SeedRealIdentityAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Mock.Of<ICurrentUserService>());
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connection));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = Email,
            Email = Email,
            FirstName = "QA",
            LastName = "Lockout",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, CorrectPassword);
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
            signInManager,
            new JwtTokenGenerator(jwtOptions),
            NullLogger<IdentityService>.Instance);

        return (identityService, userManager, user, context);
    }

    [Fact]
    public async Task Correct_Password_Authenticates_Normally_When_Not_Locked_Out()
    {
        var (identityService, _, _, _) = await SeedRealIdentityAsync();

        var login = await identityService.LoginAsync(Email, CorrectPassword);

        login.Should().NotBeNull();
        login.Token.Should().NotBeNullOrEmpty();
        login.Email.Should().Be(Email);
    }

    [Fact]
    public async Task Fifth_Failed_Attempt_Locks_The_Account_And_The_Sixth_Attempt_Is_Rejected_Even_With_The_Correct_Password()
    {
        var (identityService, userManager, _, _) = await SeedRealIdentityAsync();

        for (var i = 1; i <= 5; i++)
        {
            var act = async () => await identityService.LoginAsync(Email, WrongPassword);
            await act.Should().ThrowAsync<UnauthorizedException>(
                $"attempt {i} is a plain wrong-password rejection");
        }

        var afterFifth = await userManager.FindByEmailAsync(Email);
        (await userManager.IsLockedOutAsync(afterFifth!)).Should().BeTrue(
            "the fifth failed attempt must cross Identity's default MaxFailedAccessAttempts threshold");
        afterFifth!.LockoutEnd.Should().NotBeNull();

        var sixthAttempt = async () => await identityService.LoginAsync(Email, CorrectPassword);
        await sixthAttempt.Should().ThrowAsync<UnauthorizedException>(
            "a locked account must reject even the correct password until the lockout expires")
            .WithMessage("Correo o contraseña incorrectos.");
    }

    [Fact]
    public async Task After_Lockout_Expires_The_Correct_Password_Succeeds_And_AccessFailedCount_Resets()
    {
        var (identityService, userManager, user, _) = await SeedRealIdentityAsync();

        for (var i = 0; i < 5; i++)
        {
            try { await identityService.LoginAsync(Email, WrongPassword); }
            catch (UnauthorizedException) { }
        }

        (await userManager.IsLockedOutAsync(user)).Should().BeTrue("5 failed attempts must have locked the account");

        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1));

        (await userManager.IsLockedOutAsync(user)).Should().BeFalse("an expired LockoutEnd must no longer count as locked out");

        var login = await identityService.LoginAsync(Email, CorrectPassword);
        login.Token.Should().NotBeNullOrEmpty("the correct password must succeed again once the lockout window has passed");

        var afterSuccess = await userManager.FindByEmailAsync(Email);
        afterSuccess!.AccessFailedCount.Should().Be(0,
            "a successful sign-in must reset AccessFailedCount, exactly like ASP.NET Identity's own contract");
    }

    [Fact]
    public async Task Nonexistent_User_Wrong_Password_And_Locked_Out_User_Share_The_Same_401_Message()
    {
        var (identityService, userManager, user, _) = await SeedRealIdentityAsync();
        
        var nonexistent = async () => await identityService.LoginAsync("qa-does-not-exist@test.local", WrongPassword);
        var nonexistentEx = await nonexistent.Should().ThrowAsync<UnauthorizedException>();
        var wrongPassword = async () => await identityService.LoginAsync(Email, WrongPassword);
        var wrongPasswordEx = await wrongPassword.Should().ThrowAsync<UnauthorizedException>();

        for (var i = 0; i < 4; i++)
        {
            try { await identityService.LoginAsync(Email, WrongPassword); }
            catch (UnauthorizedException) { }
        }
        (await userManager.IsLockedOutAsync(user)).Should().BeTrue();

        var lockedOut = async () => await identityService.LoginAsync(Email, CorrectPassword);
        var lockedOutEx = await lockedOut.Should().ThrowAsync<UnauthorizedException>();

        const string expectedMessage = "Correo o contraseña incorrectos.";

        nonexistentEx.Which.Message.Should().Be(expectedMessage);
        wrongPasswordEx.Which.Message.Should().Be(expectedMessage);
        lockedOutEx.Which.Message.Should().Be(expectedMessage,
            "a locked-out account must be indistinguishable from a plain wrong password — a different message would let an attacker use the lockout itself to enumerate registered emails");
    }

    // Regression coverage for the Punto 5 / Escenario I finding: the tests
    // above call IdentityService.LoginAsync directly, bypassing MediatR
    // entirely — they prove the lockout mechanism itself is correct, but
    // never exercised the real path (LoginCommand -> TransactionBehavior ->
    // LoginCommandHandler) where the bug actually lived: TransactionBehavior
    // rolled back the AccessFailedCount/LockoutEnd bookkeeping on every
    // failed login, because UnauthorizedException is one of its recognized
    // "expected business exceptions". This test drives the exact same
    // pipeline the real API uses.
    [Fact]
    public async Task Through_The_Real_LoginCommand_Pipeline_Failed_Attempts_Persist_And_Lock_The_Account()
    {
        var (identityService, userManager, _, context) = await SeedRealIdentityAsync();

        var handler = new LoginCommandHandler(identityService);
        var behavior = new TransactionBehavior<LoginCommand, LoginResponseDto>(
            context, NullLogger<TransactionBehavior<LoginCommand, LoginResponseDto>>.Instance);

        Func<LoginCommand, Task<LoginResponseDto>> runThroughPipeline = command =>
            behavior.Handle(command, _ => handler.Handle(command, CancellationToken.None), CancellationToken.None);

        for (var i = 1; i <= 5; i++)
        {
            var command = new LoginCommand { Login = new LoginRequestDto { Email = Email, Password = WrongPassword } };
            var act = async () => await runThroughPipeline(command);

            await act.Should().ThrowAsync<UnauthorizedException>(
                $"attempt {i} through the real pipeline is a plain wrong-password rejection");
        }

        // ASP.NET Identity's own AccessFailedAsync resets AccessFailedCount back
        // to 0 in the exact same update that trips the lockout — LockoutEnd is
        // what actually carries the "locked" state going forward, not the
        // counter. So the meaningful assertion here is LockoutEnd/IsLockedOut,
        // not a literal AccessFailedCount == 5 (which would never be true even
        // without the Escenario I bug).
        var afterFifth = await userManager.FindByEmailAsync(Email);

        (await userManager.IsLockedOutAsync(afterFifth!)).Should().BeTrue(
            "TransactionBehavior must not roll back the failed-attempt bookkeeping for LoginCommand — that is exactly the Escenario I defect. Without the fix, every attempt above would have reset AccessFailedCount to 0 and the account would never lock.");
        afterFifth!.LockoutEnd.Should().NotBeNull();

        var sixthCommand = new LoginCommand { Login = new LoginRequestDto { Email = Email, Password = CorrectPassword } };
        var sixthAttempt = async () => await runThroughPipeline(sixthCommand);

        await sixthAttempt.Should().ThrowAsync<UnauthorizedException>(
            "a locked account must reject even the correct password through the real pipeline")
            .WithMessage("Correo o contraseña incorrectos.");
    }

    [Fact]
    public async Task Through_The_Real_LoginCommand_Pipeline_A_Correct_Password_On_An_Unlocked_Account_Still_Succeeds()
    {
        var (identityService, _, _, context) = await SeedRealIdentityAsync();

        var handler = new LoginCommandHandler(identityService);
        var behavior = new TransactionBehavior<LoginCommand, LoginResponseDto>(
            context, NullLogger<TransactionBehavior<LoginCommand, LoginResponseDto>>.Instance);

        var command = new LoginCommand { Login = new LoginRequestDto { Email = Email, Password = CorrectPassword } };

        var response = await behavior.Handle(command, _ => handler.Handle(command, CancellationToken.None), CancellationToken.None);

        response.Should().NotBeNull();
        response.Token.Should().NotBeNullOrEmpty(
            "the opt-out must not break the ordinary successful-login path");
    }
}
