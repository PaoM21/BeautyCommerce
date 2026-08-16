using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeautyCommerce.Infrastructure.Configurations.EntityConfigurations;

public class OrderConfiguration
    : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", t =>
        {
            t.HasCheckConstraint("CK_Orders_Total_NonNegative", "\"Total\" >= 0");
            t.HasCheckConstraint("CK_Orders_SubTotal_NonNegative", "\"SubTotal\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber)
            .HasMaxLength(50);

        builder.Property(x => x.SubTotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.Tax)
            .HasPrecision(18, 2);

        builder.Property(x => x.ShippingCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.Total)
            .HasPrecision(18, 2);
    }
}