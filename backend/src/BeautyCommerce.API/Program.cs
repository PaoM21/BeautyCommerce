using BeautyCommerce.API.Middlewares;
using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Settings;
using BeautyCommerce.Application.Features.Brands.Commands.CreateBrand;
using BeautyCommerce.Infrastructure.Configurations;
using BeautyCommerce.Infrastructure.Identity;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace BeautyCommerce.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog();

        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IIdentityService, IdentityService>();
        builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        builder.Services.AddScoped<IInventoryService, InventoryService>();
        builder.Services.AddScoped<IProductVariantIdentifierGenerator, ProductVariantIdentifierGenerator>();
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<BeautyCommerce.Application.Common.Interfaces.ICacheService, BeautyCommerce.Infrastructure.Services.MemoryCacheService>();

        builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(BeautyCommerce.Application.Common.Behaviors.LoggingBehavior<,>));
        builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(BeautyCommerce.Application.Common.Behaviors.PerformanceBehavior<,>));
        builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(BeautyCommerce.Application.Common.Behaviors.TransactionBehavior<,>));
        builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(BeautyCommerce.Application.Common.Behaviors.CachingBehavior<,>));
        builder.Services.AddHostedService<BeautyCommerce.Infrastructure.Services.OutboxProcessor>();

        builder.Services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddErrorDescriber<SpanishIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateBrandCommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        builder.Services.AddFluentValidationAutoValidation();

        builder.Services.AddValidatorsFromAssembly(
            Assembly.Load("BeautyCommerce.Application"));

        builder.Services.AddOptions<JwtSettings>()
            .Bind(builder.Configuration.GetSection("Jwt"))
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Key) && Encoding.UTF8.GetByteCount(settings.Key) >= 16,
                "Jwt:Key debe estar presente y tener al menos 16 bytes (128 bits), el mínimo requerido por el algoritmo HS256.")
            .ValidateOnStart();

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = key
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            });

        builder.Services.AddOptions<CorsSettings>()
            .Bind(builder.Configuration.GetSection("Cors"))
            .Validate(
                settings => settings.AllowedOrigins is { Length: > 0 } &&
                            settings.AllowedOrigins.All(origin => !string.IsNullOrWhiteSpace(origin)),
                "Cors:AllowedOrigins debe contener al menos un origin no vacío.")
            .ValidateOnStart();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DefaultCorsPolicy", policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod()
                    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>());
            });
        });

        builder.Services.AddHealthChecks();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BeautyCommerce API",
                Version = "v1"
            });

            c.CustomSchemaIds(type => type.FullName);

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese el JWT. Ejemplo: eyJhbGciOiJIUzI1NiIs..."
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });


        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            using var migrationScope = app.Services.CreateScope();

            var dbContext = migrationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            dbContext.Database.Migrate();
        }

        app.UseExceptionHandler();

        app.UseSerilogRequestLogging();

        app.UseStaticFiles();
        app.UseRouting();

        app.UseCors("DefaultCorsPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.Run();
    }
}
