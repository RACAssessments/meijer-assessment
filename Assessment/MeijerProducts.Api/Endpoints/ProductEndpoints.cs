using MeijerProducts.Api.Contracts;
using MeijerProducts.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MeijerProducts.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Products");

        group.MapGet("/", async (ProductDbContext db) =>
        {
            var products = await db.Products
                .AsNoTracking()
                .Select(p => new ProductListDto(p.Id, p.ImageUrl, p.Summary, p.Title))
                .ToListAsync();

            return Results.Ok(products);
        })
        .WithName("GetProducts")
        .WithSummary("Get all products")
        .Produces<List<ProductListDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", async (int id, ProductDbContext db) =>
        {
            var product = await db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            return product is null
                ? Results.NotFound()
                : Results.Ok(product.ToDetailDto());
        })
        .WithName("GetProductById")
        .WithSummary("Get a single product by id")
        .Produces<ProductDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return group;
    }
}
