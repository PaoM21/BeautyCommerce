using BeautyCommerce.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Tests.Helpers;

// EF Core's InMemory provider (see DbContextHelper) doesn't support
// ExecuteUpdateAsync/ExecuteDeleteAsync — it throws
// "not supported by the current database provider". InventoryService now
// uses ExecuteUpdateAsync for its atomic stock increment/decrement (see
// 6.2.2), so tests exercising it need a real relational provider. SQLite's
// in-memory mode gives that without requiring a live Postgres instance,
// keeping these tests fast and CI-friendly.
public static class SqliteDbContextHelper
{
    public static (ApplicationDbContext Context, SqliteConnection Connection) CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options, null);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
