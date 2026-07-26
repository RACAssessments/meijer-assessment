using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MeijerProducts.Api.Tests;

// Points the app at a per-run temp SQLite file instead of the default products.db, so the
// startup Migrate() + DbInitializer.Seed() in Program.cs gives every test in the collection
// the same deterministic 30-product dataset with no test-side fixtures or manual seeding.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"meijer-products-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // SQLite connection pooling keeps a native handle open on the file past the point
            // WebApplicationFactory disposes the host, so the pool must be cleared before the
            // temp file can be deleted (Windows locks open files; POSIX would allow it, but
            // the leaked pooled connection would still linger for the process lifetime).
            SqliteConnection.ClearAllPools();
            File.Delete(_dbPath);
        }
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
