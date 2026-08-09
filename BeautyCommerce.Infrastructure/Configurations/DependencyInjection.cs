using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeautyCommerce.Infrastructure.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var defaultConn = configuration.GetConnectionString("DefaultConnection");

        // If the connection string looks like a Postgres connection (starts with Host=)
        // use Npgsql. Otherwise assume it's a SQLite connection string (e.g. Data Source=...) and
        // use Sqlite. This allows running the app locally with SQLite for quick testing.
        if (!string.IsNullOrWhiteSpace(defaultConn) &&
            defaultConn.TrimStart().StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(defaultConn));
        }

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Repositories
        services.AddScoped<BeautyCommerce.Application.Common.Interfaces.IProductRepository, BeautyCommerce.Infrastructure.Repositories.ProductRepository>();

        // Services
        services.AddScoped<BeautyCommerce.Application.Common.Interfaces.IUserService, BeautyCommerce.Infrastructure.Services.UserService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}