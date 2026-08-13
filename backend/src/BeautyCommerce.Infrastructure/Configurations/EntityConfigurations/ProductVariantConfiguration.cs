using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeautyCommerce.Infrastructure.Configurations.EntityConfigurations;

public class ProductVariantConfiguration
    : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants", t =>
        {
            t.HasCheckConstraint("CK_ProductVariants_Stock_NonNegative", "\"Stock\" >= 0");
            t.HasCheckConstraint("CK_ProductVariants_Price_NonNegative", "\"Price\" >= 0");
            t.HasCheckConstraint("CK_ProductVariants_Cost_NonNegative", "\"Cost\" >= 0");
            t.HasCheckConstraint("CK_ProductVariants_OldPrice_NonNegative", "\"OldPrice\" IS NULL OR \"OldPrice\" >= 0");
            t.HasCheckConstraint("CK_ProductVariants_MinimumStock_NonNegative", "\"MinimumStock\" >= 0");
            t.HasCheckConstraint("CK_ProductVariants_SKU_NotBlank", "length(trim(\"SKU\")) > 0");
            t.HasCheckConstraint("CK_ProductVariants_Barcode_NotBlank", "length(trim(\"Barcode\")) > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SKU)
            .HasMaxLength(50);

        builder.HasIndex(x => x.SKU)
            .IsUnique();

        builder.Property(x => x.Barcode)
            .HasMaxLength(100);

        builder.HasIndex(x => x.Barcode)
            .IsUnique();

        builder.Property(x => x.Color)
            .HasMaxLength(100);

        builder.Property(x => x.Size)
            .HasMaxLength(100);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);

        builder.Property(x => x.Cost)
            .HasPrecision(18, 2);

        builder.Property(x => x.OldPrice)
            .HasPrecision(18, 2);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}