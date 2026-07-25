using MeijerProducts.Api.Data;
using MeijerProducts.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "Default";

builder.Services.AddDbContext<ProductDbContext>(options =>
{
    // ConnectionStrings:Default, overridable in Docker via the ConnectionStrings__Default
    // env var — ASP.NET Core's default configuration providers already map "__" to ":" and
    // env vars are layered after appsettings.json, so no extra code is needed for the override.
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=products.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        // Intentionally broad for this take-home/demo API: no auth, and the MAUI client
        // runs from device/emulator/simulator origins that aren't a fixed known set.
        // Do not carry an AllowAnyOrigin policy like this into a real production service.
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

// Enabled unconditionally, not gated to Development, so it's still reachable when the
// container defaults to the Production environment (see #12).
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);

// Guarded so `dotnet ef migrations add` (which spins the host up to build the model but
// must not touch a real database) doesn't execute this block.
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.Migrate();
    DbInitializer.Seed(db);
}

app.MapProductEndpoints();

app.Run();
