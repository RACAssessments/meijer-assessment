using MeijerProducts.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MeijerProducts.Api.Tests.Data;

public class DbInitializerTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ProductDbContext _db;

    public DbInitializerTests()
    {
        _connection.Open();

        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ProductDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void Seed_PopulatesThirtyProductsWithIdsZeroThroughTwentyNine()
    {
        DbInitializer.Seed(_db);

        var ids = _db.Products.Select(p => p.Id).OrderBy(id => id).ToList();

        Assert.Equal(30, ids.Count);
        Assert.Equal(Enumerable.Range(0, 30), ids);
    }

    [Fact]
    public void Seed_CalledTwice_DoesNotDuplicateRows()
    {
        DbInitializer.Seed(_db);
        DbInitializer.Seed(_db);

        Assert.Equal(30, _db.Products.Count());
    }

    [Fact]
    public void Seed_EveryProduct_HasAllRequiredColumnsPopulated()
    {
        DbInitializer.Seed(_db);

        foreach (var product in _db.Products.ToList())
        {
            Assert.False(string.IsNullOrWhiteSpace(product.Title));
            Assert.False(string.IsNullOrWhiteSpace(product.Summary));
            Assert.False(string.IsNullOrWhiteSpace(product.Description));
            Assert.False(string.IsNullOrWhiteSpace(product.Price));
            Assert.False(string.IsNullOrWhiteSpace(product.ImageUrl));
        }
    }
}
