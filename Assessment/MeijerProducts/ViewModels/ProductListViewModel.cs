using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeijerProducts.Helpers;
using MeijerProducts.Models;
using MeijerProducts.Services;

namespace MeijerProducts.ViewModels;

public partial class ProductListViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public ObservableCollection<ProductSummary> Products { get; } = [];

    public ProductListViewModel(IProductService productService, INavigationService navigationService)
    {
        _productService = productService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var products = await _productService.GetProductsAsync();

            Products.Clear();
            foreach (var product in products)
            {
                Products.Add(product);
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to load products. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToDetailsAsync(ProductSummary? product)
    {
        if (product is null)
        {
            return;
        }

        await _navigationService.GoToAsync($"{Routes.ProductDetail}?id={product.Id}");
    }
}
