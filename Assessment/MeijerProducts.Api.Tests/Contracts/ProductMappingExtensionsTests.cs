using MeijerProducts.Api.Contracts;
using MeijerProducts.Api.Data;
using Xunit;

namespace MeijerProducts.Api.Tests.Contracts;

public class ProductMappingExtensionsTests
{
    [Fact]
    public void ToDetailDto_MapsEachFieldToItsOwnPositionNotTransposed()
    {
        var product = new Product
        {
            Id = 7,
            Title = "the-title",
            Summary = "the-summary",
            Description = "the-description",
            Price = "the-price",
            ImageUrl = "the-image-url",
        };

        var dto = product.ToDetailDto();

        Assert.Equal(7, dto.Id);
        Assert.Equal("the-image-url", dto.ImageUrl);
        Assert.Equal("the-summary", dto.Summary);
        Assert.Equal("the-title", dto.Title);
        Assert.Equal("the-description", dto.Description);
        Assert.Equal("the-price", dto.Price);
    }
}
