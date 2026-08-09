using BeautyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Tests.Helpers;

public static class ApplicationDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // ApplicationDbContext constructor accepts ICurrentUserService? as second param in the solution
        return new ApplicationDbContext(options, null);
    }
}
