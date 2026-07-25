using MeijerProducts.Api.Data;

namespace MeijerProducts.Api.Contracts;

public static class ProductMappingExtensions
{
    public static ProductDetailDto ToDetailDto(this Product product) =>
        new(product.Id, product.ImageUrl, product.Summary, product.Title, product.Description, product.Price);
}
