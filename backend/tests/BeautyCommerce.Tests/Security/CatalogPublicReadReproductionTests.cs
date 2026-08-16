using System.Reflection;
using System.Security.Claims;
using BeautyCommerce.API.Controllers;
using BeautyCommerce.Application.Common.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeautyCommerce.Tests.Security;

public class CatalogPublicReadReproductionTests
{
    private static async Task<bool> IsAllowedAsync(Type controllerType, string actionName, ClaimsPrincipal user)
    {
        var method = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull($"{controllerType.Name}.{actionName} should exist");

        // [AllowAnonymous] always wins over any [Authorize], on the method
        // or the class — this is standard, documented ASP.NET Core
        // behavior, not something being guessed at here.
        var hasAllowAnonymous =
            method!.GetCustomAttributes<AllowAnonymousAttribute>().Any() ||
            controllerType.GetCustomAttributes<AllowAnonymousAttribute>().Any();

        if (hasAllowAnonymous)
            return true;

        var authorizeAttributes = method.GetCustomAttributes<AuthorizeAttribute>()
            .Concat(controllerType.GetCustomAttributes<AuthorizeAttribute>())
            .Cast<IAuthorizeData>()
            .ToList();

        if (authorizeAttributes.Count == 0)
            return true; // no [Authorize] anywhere on the action = public

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddAuthorization();
        await using var provider = services.BuildServiceProvider();

        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await AuthorizationPolicy.CombineAsync(policyProvider, authorizeAttributes);

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var result = await authService.AuthorizeAsync(user, policy!);

        return result.Succeeded;
    }

    private static ClaimsPrincipal AnonymousUser() =>
        new(new ClaimsIdentity()); // no authentication type => IsAuthenticated == false, exactly like a request with no/invalid JWT

    private static ClaimsPrincipal AuthenticatedUser(string? role = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        if (role != null)
            claims.Add(new Claim(ClaimTypes.Role, role));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Bearer"));
    }

    [Fact]
    public async Task Sanity_Authenticated_NonAdmin_Is_Denied_On_Brands_Create()
    {
        var allowed = await IsAllowedAsync(typeof(BrandsController), "Create", AuthenticatedUser());
        allowed.Should().BeFalse("a logged-in customer without the Admin role must not be able to create a brand");
    }

    [Fact]
    public async Task Sanity_Authenticated_Admin_Is_Allowed_On_Brands_Create()
    {
        var allowed = await IsAllowedAsync(typeof(BrandsController), "Create", AuthenticatedUser(Roles.Admin));
        allowed.Should().BeTrue("an admin must be able to create a brand");
    }

    [Fact]
    public async Task Anonymous_User_Is_Allowed_On_Brands_GetAll()
    {
        var allowed = await IsAllowedAsync(typeof(BrandsController), "GetAll", AnonymousUser());
        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task Anonymous_User_Is_Allowed_On_Products_GetProducts_And_GetById()
    {
        (await IsAllowedAsync(typeof(ProductsController), "GetProducts", AnonymousUser())).Should().BeTrue();
        (await IsAllowedAsync(typeof(ProductsController), "GetById", AnonymousUser())).Should().BeTrue();
    }

    [Fact]
    public async Task Anonymous_User_Is_Allowed_On_Brands_GetById()
    {
        var allowed = await IsAllowedAsync(typeof(BrandsController), "GetById", AnonymousUser());
        allowed.Should().BeTrue(
            "BrandsController.GetById now has [AllowAnonymous], matching BrandsController.GetAll — same catalog data, same access contract");
    }

    [Fact]
    public async Task Anonymous_User_Is_Allowed_On_Categories_GetAll()
    {
        var allowed = await IsAllowedAsync(typeof(CategoriesController), "GetAll", AnonymousUser());
        allowed.Should().BeTrue(
            "CategoriesController.GetAll now has [AllowAnonymous], matching the equivalent Brands/Products catalog reads");
    }

    [Fact]
    public async Task Anonymous_User_Is_Allowed_On_Categories_GetById()
    {
        var allowed = await IsAllowedAsync(typeof(CategoriesController), "GetById", AnonymousUser());
        allowed.Should().BeTrue("CategoriesController.GetById now has [AllowAnonymous], closing the last inconsistent catalog read");
    }

    [Fact]
    public async Task Authenticated_User_Of_Any_Role_Is_Allowed_On_The_Three_Reads()
    {
        var user = AuthenticatedUser(); // no role at all — plain registered customer
        (await IsAllowedAsync(typeof(BrandsController), "GetById", user)).Should().BeTrue();
        (await IsAllowedAsync(typeof(CategoriesController), "GetAll", user)).Should().BeTrue();
        (await IsAllowedAsync(typeof(CategoriesController), "GetById", user)).Should().BeTrue();
    }
}
