using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeautyCommerce.Infrastructure.Identity;

// 6.4.6-A: IdentityDbContext's own OnModelCreating (called via base.OnModelCreating
// in ApplicationDbContext, before this configuration runs) declares EmailIndex on
// NormalizedEmail as a plain, non-unique index — RequireUniqueEmail = true in
// Program.cs only enforces uniqueness at the application layer (UserManager),
// never in PostgreSQL. The only real DB-level protection today is the unique
// UserNameIndex on NormalizedUserName, which happens to also protect Email only
// because IdentityService.RegisterAsync sets UserName = Email — not because
// anything stops the two from diverging (Identity's SetEmailAsync/UpdateAsync
// are unused today but fully available). This redefines the existing EmailIndex
// as UNIQUE instead of adding a redundant second index.
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();
    }
}
