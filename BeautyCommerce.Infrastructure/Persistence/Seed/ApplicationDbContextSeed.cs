using BeautyCommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace BeautyCommerce.Infrastructure.Persistence.Seed;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Admin"
            });
        }

        if (!await roleManager.RoleExistsAsync("Customer"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Customer"
            });
        }

        if (!await roleManager.RoleExistsAsync("Seller"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Seller"
            });
        }
    }
}