using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeautyCommerce.Infrastructure.Configurations.EntityConfigurations;

public class ShoppingCartItemConfiguration
    : IEntityTypeConfiguration<ShoppingCartItem>
{
    public void Configure(EntityTypeBuilder<ShoppingCartItem> builder)
    {
        builder.ToTable("ShoppingCartItems", t =>
        {
            t.HasCheckConstraint("CK_ShoppingCartItems_Quantity_Positive", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_ShoppingCartItems_UnitPrice_NonNegative", "\"UnitPrice\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}