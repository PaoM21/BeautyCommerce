using System.Reflection;
using BeautyCommerce.API.Controllers;
using BeautyCommerce.Application.Common.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace BeautyCommerce.Tests.Security;

public class CatalogAuthorizationTests
{
    // Found via live testing: Products/Brands/Categories only had a
    // class-level [Authorize] (any authenticated user), with no
    // [Authorize(Roles = Admin)] on the write actions. A plain registered
    // customer could — and, in testing, did — create/rename/delete a real
    // category through the API even though the admin UI never exposes
    // those actions to non-admins. This test locks the fix in so nobody
    // can silently drop the role check while refactoring these controllers.
    [Theory]
    [InlineData(typeof(ProductsController), "Create")]
    [InlineData(typeof(ProductsController), "Update")]
    [InlineData(typeof(ProductsController), "Delete")]
    [InlineData(typeof(BrandsController), "Create")]
    [InlineData(typeof(BrandsController), "Update")]
    [InlineData(typeof(BrandsController), "Delete")]
    [InlineData(typeof(CategoriesController), "Create")]
    [InlineData(typeof(CategoriesController), "Update")]
    [InlineData(typeof(CategoriesController), "Delete")]
    public void Write_Action_Requires_Admin_Role(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            $"{controllerType.Name}.{methodName} should exist");

        var authorizeAttributes = method!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Concat(controllerType.GetCustomAttributes<AuthorizeAttribute>())
            .ToList();

        authorizeAttributes
            .Any(x => x.Roles != null && x.Roles.Contains(Roles.Admin))
            .Should().BeTrue(
                $"{controllerType.Name}.{methodName} must require the Admin role " +
                "— it mutates catalog data that only admins should be able to change.");
    }
}
