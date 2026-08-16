using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeautyCommerce.Infrastructure.Configurations.EntityConfigurations;

public class InventoryMovementConfiguration
    : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements", t =>
        {
            t.HasCheckConstraint("CK_InventoryMovements_Quantity_Positive", "\"Quantity\" > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason)
            .HasMaxLength(300);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId);
    }
}