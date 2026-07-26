using System.Net;
using System.Text.Json;
using Xunit;

namespace MeijerProducts.Api.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public class ProductDetailEndpointTests(ApiFactory factory)
{
    private static readonly string[] ExpectedFields = ["id", "imageUrl", "summary", "title", "description", "price"];

    [Fact]
    public async Task GetProductById_WithSeededId_ReturnsOkWithSeededValues()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/products/0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(0, root.GetProperty("id").GetInt32());
        Assert.Equal("Bananas", root.GetProperty("title").GetString());
        Assert.Equal("Fresh bananas, perfect for a healthy snack.", root.GetProperty("summary").GetString());
        Assert.Equal("$0.59/lb", root.GetProperty("price").GetString());
    }

    [Fact]
    public async Task GetProductById_HasExactlySixFieldsAndPriceIsAJsonString()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/products/0");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        var propertyNames = root.EnumerateObject().Select(p => p.Name).Order().ToArray();
        Assert.Equal(ExpectedFields.Order(), propertyNames);
        Assert.Equal(JsonValueKind.String, root.GetProperty("price").ValueKind);
    }

    [Theory]
    [InlineData("/products/30")]
    [InlineData("/products/-1")]
    [InlineData("/products/abc")]
    public async Task GetProductById_WithMissingOrInvalidId_ReturnsNotFound(string requestUri)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
