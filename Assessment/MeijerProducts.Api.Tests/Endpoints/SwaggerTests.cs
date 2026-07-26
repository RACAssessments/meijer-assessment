using System.Net;
using Xunit;

namespace MeijerProducts.Api.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public class SwaggerTests(ApiFactory factory)
{
    [Fact]
    public async Task SwaggerJson_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
