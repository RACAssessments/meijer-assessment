using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MeijerProducts.Models;

namespace MeijerProducts.Services;

public class ProductService : IProductService
{
    // The API serializes with ASP.NET Core's web defaults (camelCase), while these models use
    // idiomatic PascalCase C# property names, so deserialization needs the matching options.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public ProductService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductSummary>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _httpClient.GetFromJsonAsync<List<ProductSummary>>("products", JsonOptions, cancellationToken);
        return products ?? [];
    }

    public async Task<ProductDetail?> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"products/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductDetail>(JsonOptions, cancellationToken);
    }
}
