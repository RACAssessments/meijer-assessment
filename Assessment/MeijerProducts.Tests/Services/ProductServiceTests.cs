using System.Net;
using MeijerProducts.Services;
using MeijerProducts.Tests.TestDoubles;
using Xunit;

namespace MeijerProducts.Tests.Services;

public class ProductServiceTests
{
    // The API serializes with ASP.NET Core's web defaults, so the wire format is camelCase
    // while the client models are PascalCase - that mapping is what these tests pin down.
    private const string ProductListJson = """
        [
          { "id": 0, "imageUrl": "https://example.com/bananas.png", "summary": "Fresh bananas.", "title": "Bananas" },
          { "id": 1, "imageUrl": "https://example.com/apples.png", "summary": "Crisp apples.", "title": "Gala Apples" }
        ]
        """;

    private const string ProductDetailJson = """
        {
          "id": 0,
          "imageUrl": "https://example.com/bananas.png",
          "summary": "Fresh bananas.",
          "title": "Bananas",
          "description": "Fresh bananas, perfect for a healthy snack.",
          "price": "$0.59/lb"
        }
        """;

    [Fact]
    public async Task GetProductsAsync_WhenApiReturnsProducts_DeserializesCamelCaseJson()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson(HttpStatusCode.OK, ProductListJson);
        var service = new ProductService(handler.CreateClient());

        var products = await service.GetProductsAsync();

        Assert.Equal(2, products.Count);
        Assert.Equal(0, products[0].Id);
        Assert.Equal("Bananas", products[0].Title);
        Assert.Equal("Fresh bananas.", products[0].Summary);
        Assert.Equal("https://example.com/bananas.png", products[0].ImageUrl);
        Assert.Equal("Gala Apples", products[1].Title);
    }

    [Fact]
    public async Task GetProductsAsync_WhenApiReturnsEmptyArray_ReturnsEmptyList()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson(HttpStatusCode.OK, "[]");
        var service = new ProductService(handler.CreateClient());

        var products = await service.GetProductsAsync();

        Assert.Empty(products);
    }

    [Fact]
    public async Task GetProductsAsync_WhenApiReturnsJsonNull_ReturnsEmptyListNotNull()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson(HttpStatusCode.OK, "null");
        var service = new ProductService(handler.CreateClient());

        var products = await service.GetProductsAsync();

        Assert.NotNull(products);
        Assert.Empty(products);
    }

    [Fact]
    public async Task GetProductsAsync_RequestsProductsRelativeToBaseAddress()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson(HttpStatusCode.OK, "[]");
        var service = new ProductService(handler.CreateClient());

        await service.GetProductsAsync();

        Assert.Equal("http://localhost:5217/products", handler.LastRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetProductsAsync_WhenApiReturnsServerError_ThrowsHttpRequestException()
    {
        var handler = FakeHttpMessageHandler.RespondingWith(HttpStatusCode.InternalServerError);
        var service = new ProductService(handler.CreateClient());

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetProductsAsync());
    }

    [Fact]
    public async Task GetProductAsync_WhenApiReturnsProduct_DeserializesAllDetailFields()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson(HttpStatusCode.OK, ProductDetailJson);
        var service = new ProductService(handler.CreateClient());

        var product = await service.GetProductAsync(0);

        Assert.NotNull(product);
        Assert.Equal(0, product.Id);
        Assert.Equal("Bananas", product.Title);
        Assert.Equal("Fresh bananas.", product.Summary);
        Assert.Equal("Fresh bananas, perfect for a healthy snack.", product.Description);
        Assert.Equal("$0.59/lb", product.Price);
        Assert.Equal("https://example.com/bananas.png", product.ImageUrl);
    }

    [Fact]
    public async Task GetProductAsync_WhenApiReturnsNotFound_ReturnsNull()
    {
        var handler = FakeHttpMessageHandler.RespondingWith(HttpStatusCode.NotFound);
        var service = new ProductService(handler.CreateClient());

        var product = await service.GetProductAsync(9999);

        Assert.Null(product);
    }

    [Fact]
    public async Task GetProductAsync_WhenApiReturnsServerError_ThrowsHttpRequestException()
    {
        var handler = FakeHttpMessageHandler.RespondingWith(HttpStatusCode.InternalServerError);
        var service = new ProductService(handler.CreateClient());

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetProductAsync(0));
    }

    [Fact]
    public async Task GetProductAsync_RequestsProductByIdPath()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson(HttpStatusCode.OK, ProductDetailJson);
        var service = new ProductService(handler.CreateClient());

        await service.GetProductAsync(7);

        Assert.Equal("http://localhost:5217/products/7", handler.LastRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetProductAsync_WhenCancelled_DoesNotSendRequest()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson(HttpStatusCode.OK, ProductDetailJson);
        var service = new ProductService(handler.CreateClient());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetProductAsync(0, cts.Token));

        Assert.Empty(handler.Requests);
    }
}
