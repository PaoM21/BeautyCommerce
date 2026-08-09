using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeautyCommerce.Infrastructure.Configurations.EntityConfigurations;

public class ShoppingCartConfiguration
    : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.ToTable("ShoppingCarts");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.User)
            .WithOne(x => x.ShoppingCart)
            .HasForeignKey<ShoppingCart>(x => x.UserId);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.ShoppingCart)
            .HasForeignKey(x => x.ShoppingCartId);
    }
}