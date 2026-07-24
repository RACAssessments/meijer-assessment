using MeijerProducts.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ProductDbContext>(options =>
{
    // ConnectionStrings:Default, overridable in Docker via the ConnectionStrings__Default
    // env var — ASP.NET Core's default configuration providers already map "__" to ":" and
    // env vars are layered after appsettings.json, so no extra code is needed for the override.
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=products.db";
    options.UseSqlite(connectionString);
});

var app = builder.Build();

app.UseHttpsRedirection();

// Guarded so `dotnet ef migrations add` (which spins the host up to build the model but
// must not touch a real database) doesn't execute this block.
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.Migrate();
}

app.Run();
