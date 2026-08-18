using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
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
        var defaultConn = ConnectionStringHelper.Normalize(
            configuration.GetConnectionString("DefaultConnection"));

        if (!string.IsNullOrWhiteSpace(defaultConn) &&
            defaultConn.TrimStart().StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(defaultConn));
        }

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<BeautyCommerce.Application.Common.Interfaces.IProductRepository, BeautyCommerce.Infrastructure.Repositories.ProductRepository>();

        services.AddScoped<BeautyCommerce.Application.Common.Interfaces.IUserService, BeautyCommerce.Infrastructure.Services.UserService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.Configure<GoogleDriveOptions>(configuration.GetSection("GoogleDrive"));
        services.Configure<CloudinaryOptions>(configuration.GetSection("Cloudinary"));
        services.AddScoped<IDriveFileProvider, GoogleDriveFileProvider>();
        services.AddScoped<IImageUploader, CloudinaryImageUploader>();

        services.Configure<ShippingRatesOptions>(configuration.GetSection("Shipping"));
        services.AddScoped<IShippingCostCalculator, ShippingCostCalculator>();

        return services;
    }
}