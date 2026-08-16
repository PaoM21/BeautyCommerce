using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Infrastructure.Identity;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Integrity;

public class SpanishIdentityErrorDescriberTests
{
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
            .AddErrorDescriber<SpanishIdentityErrorDescriber>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        return (
            provider.GetRequiredService<UserManager<ApplicationUser>>(),
            provider.GetRequiredService<SignInManager<ApplicationUser>>());
    }

    [Fact]
    public async Task Weak_Password_Registration_Fails_With_Spanish_Messages_Covering_Every_Violated_Rule()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var (userManager, signInManager) = BuildIdentityManagers(context);
        var identityService = new IdentityService(
            userManager, signInManager, Mock.Of<IJwtTokenGenerator>(), NullLogger<IdentityService>.Instance);

        var act = async () => await identityService.RegisterAsync("QA", "User", "qa-weak@test.com", "abc");

        var thrown = await act.Should().ThrowAsync<BadRequestException>();
        var message = thrown.Which.Message;

        message.Should().NotContain("Password", "the message must be fully translated, not a mix of English and Spanish");
        message.Should().Contain("La contraseña debe tener al menos 8 caracteres.");
        message.Should().Contain("dígito");
        message.Should().Contain("mayúscula");
    }

    [Fact]
    public async Task Strong_Password_Still_Registers_Successfully_Policy_Was_Not_Weakened()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        context.Roles.Add(new ApplicationRole { Name = "Customer", NormalizedName = "CUSTOMER" });
        await context.SaveChangesAsync();

        var (userManager, signInManager) = BuildIdentityManagers(context);
        var identityService = new IdentityService(
            userManager, signInManager, Mock.Of<IJwtTokenGenerator>(), NullLogger<IdentityService>.Instance);

        var userId = await identityService.RegisterAsync("QA", "User", "qa-strong@test.com", "Qa12345!");

        userId.Should().NotBeEmpty();
    }
}
