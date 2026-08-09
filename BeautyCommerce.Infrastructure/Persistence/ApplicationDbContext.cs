using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Base;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;

namespace BeautyCommerce.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>,
      IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter),
                        BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : BaseEntity
    {
        builder.Entity<TEntity>()
            .HasQueryFilter(e => !e.IsDeleted);
    }

    public async Task<bool> UserExistsAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        return await Users.AnyAsync(
            u => u.Id == userId,
            cancellationToken);
    }

    DatabaseFacade IApplicationDbContext.Database => Database;

    public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        var userId = _currentUserService?.UserId;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedBy = userId;

                if (entry.Entity.IsDeleted)
                {
                    entry.Entity.DeletedAt ??= DateTime.UtcNow;
                    entry.Entity.DeletedBy = userId;
                }
            }
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            try
            {
                var inner = ex.InnerException?.Message ?? ex.GetBaseException()?.Message;
                var msg = $"SaveChangesAsync failed: {ex.Message}" + (inner != null ? $" | Inner: {inner}" : string.Empty);
                System.Diagnostics.Debug.WriteLine(msg);

                foreach (var e in ChangeTracker.Entries().Where(en => en.State != EntityState.Unchanged))
                {
                    try
                    {
                        var vals = string.Join(", ", e.CurrentValues.Properties.Select(p =>
                            $"{p.Name}={e.CurrentValues[p]?.ToString() ?? "<null>"}"));

                        System.Diagnostics.Debug.WriteLine($"Entity: {e.Entity.GetType().FullName}, State: {e.State}, Values: {vals}");
                    }
                    catch (Exception innerLogEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to log entry values: {innerLogEx.Message}");
                    }
                }
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine($"Error while logging SaveChangesAsync failure: {logEx.Message}");
            }

            // Re-throw original exception so normal flow/handlers still observe it.
            throw;
        }
    }
}