using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeautyCommerce.Infrastructure.Persistence.Configurations;

public class LoyaltyTransactionConfiguration
    : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(
        EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("LoyaltyTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Points)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(x => x.LoyaltyAccount)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.LoyaltyAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
