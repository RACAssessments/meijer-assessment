using MeijerProducts.Models;

namespace MeijerProducts.Services;

public interface IProductService
{
    Task<List<ProductSummary>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<ProductDetail?> GetProductAsync(int id, CancellationToken cancellationToken = default);
}
