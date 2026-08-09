using BeautyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Tests.Helpers;

public static class DbContextHelper
{
    public static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, null);
    }
}