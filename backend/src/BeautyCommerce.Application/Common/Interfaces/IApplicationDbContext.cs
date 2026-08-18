using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BeautyCommerce.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Brand> Brands { get; }

    DatabaseFacade Database { get; }

    DbSet<Category> Categories { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Review> Reviews { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ShoppingCart> ShoppingCarts { get; }
    DbSet<ShoppingCartItem> ShoppingCartItems { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<WishlistItem> WishlistItems { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<LoyaltyAccount> LoyaltyAccounts { get; }
    DbSet<LoyaltyTransaction> LoyaltyTransactions { get; }
    DbSet<NewsletterSubscriber> NewsletterSubscribers { get; }

    Task<bool> UserExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}