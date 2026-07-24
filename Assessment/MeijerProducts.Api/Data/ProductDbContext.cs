using Microsoft.EntityFrameworkCore;

namespace MeijerProducts.Api.Data;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);

            // Ids come from the fixed sample data set (0-29), not DB-generated identities.
            // Without this, EF treats an explicitly-assigned Id of 0 as "unset" (CLR default
            // for int) and would substitute an auto-generated key instead of preserving 0.
            entity.Property(p => p.Id).ValueGeneratedNever();

            entity.Property(p => p.Title).IsRequired();
            entity.Property(p => p.Summary).IsRequired();
            entity.Property(p => p.Description).IsRequired();
            entity.Property(p => p.Price).IsRequired();
            entity.Property(p => p.ImageUrl).IsRequired();
        });
    }
}
