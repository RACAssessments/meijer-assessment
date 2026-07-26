using System.Net;
using System.Text.Json;
using Xunit;

namespace MeijerProducts.Api.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public class ProductListEndpointTests(ApiFactory factory)
{
    private static readonly string[] ExpectedFields = ["id", "imageUrl", "summary", "title"];

    [Fact]
    public async Task GetProducts_ReturnsOkWithAllThirtySeededProducts()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.EnumerateArray().ToList();

        Assert.Equal(30, items.Count);
        Assert.Equal(Enumerable.Range(0, 30), items.Select(item => item.GetProperty("id").GetInt32()).Order());
    }

    [Fact]
    public async Task GetProducts_EachItem_HasExactlyListFieldsWithNoDescriptionOrPrice()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/products");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var firstItem = document.RootElement.EnumerateArray().First();

        var propertyNames = firstItem.EnumerateObject().Select(p => p.Name).Order().ToArray();
        Assert.Equal(ExpectedFields.Order(), propertyNames);
    }
}
