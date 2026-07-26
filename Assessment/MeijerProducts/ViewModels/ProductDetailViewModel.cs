using CommunityToolkit.Mvvm.ComponentModel;
using MeijerProducts.Models;
using MeijerProducts.Services;

namespace MeijerProducts.ViewModels;

public partial class ProductDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IProductService _productService;

    [ObservableProperty]
    public partial ProductDetail? Product { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public ProductDetailViewModel(IProductService productService)
    {
        _productService = productService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && int.TryParse(value?.ToString(), out var id))
        {
            _ = LoadProductAsync(id);
        }
    }

    private async Task LoadProductAsync(int id)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Product = await _productService.GetProductAsync(id);

            if (Product is null)
            {
                ErrorMessage = "Product not found.";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to load product details. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
