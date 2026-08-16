using BeautyCommerce.Infrastructure.Identity;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BeautyCommerce.Tests.Integrity;

public class ApplicationDbContextRedactionTests
{
    private class CapturingTraceListener : TraceListener
    {
        public List<string> Lines { get; } = new();
        public override void Write(string? message) { if (message != null) Lines.Add(message); }
        public override void WriteLine(string? message) { if (message != null) Lines.Add(message); }
    }

    [Fact]
    public async Task Failed_SaveChanges_Redacts_PasswordHash_And_SecurityStamp_But_Keeps_Other_Fields_Visible()
    {
        var (contextA, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        await using var contextB = new BeautyCommerce.Infrastructure.Persistence.ApplicationDbContext(
            new DbContextOptionsBuilder<BeautyCommerce.Infrastructure.Persistence.ApplicationDbContext>()
                .UseSqlite(connection).Options,
            null);

        var sharedId = Guid.NewGuid();

        contextA.Users.Add(new ApplicationUser
        {
            Id = sharedId,
            UserName = "qa-redaction-existing@test.com",
            Email = "qa-redaction-existing@test.com",
            PasswordHash = "EXISTING-SECRET-HASH",
            SecurityStamp = "EXISTING-SECRET-STAMP",
            FirstName = "QA",
            LastName = "Existing"
        });
        await contextA.SaveChangesAsync();

        contextB.Users.Add(new ApplicationUser
        {
            Id = sharedId,
            UserName = "qa-redaction-conflict@test.com",
            Email = "qa-redaction-conflict@test.com",
            PasswordHash = "SUPER-SECRET-HASH-VALUE",
            SecurityStamp = "SUPER-SECRET-STAMP-VALUE",
            FirstName = "QA",
            LastName = "Conflict"
        });

        var listener = new CapturingTraceListener();
        Trace.Listeners.Add(listener);

        try
        {
            var act = async () => await contextB.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>(
                "a second ApplicationUser with the same Id must violate the primary key on save");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }

        var captured = string.Join("\n", listener.Lines);

        captured.Should().Contain("PasswordHash=<redacted>");
        captured.Should().Contain("SecurityStamp=<redacted>");

        captured.Should().NotContain("SUPER-SECRET-HASH-VALUE");
        captured.Should().NotContain("SUPER-SECRET-STAMP-VALUE");

        captured.Should().Contain("qa-redaction-conflict@test.com",
            "non-sensitive fields must remain visible for diagnostic value");
    }
}
