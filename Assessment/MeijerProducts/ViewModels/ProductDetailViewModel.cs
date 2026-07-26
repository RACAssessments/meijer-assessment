using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeijerProducts.Models;
using MeijerProducts.Services;

namespace MeijerProducts.ViewModels;

public partial class ProductDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IProductService _productService;
    private readonly ILocationService _locationService;
    private readonly IShareService _shareService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToListCommand))]
    public partial ProductDetail? Product { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToListCommand))]
    public partial bool IsSharing { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public ProductDetailViewModel(
        IProductService productService,
        ILocationService locationService,
        IShareService shareService)
    {
        _productService = productService;
        _locationService = locationService;
        _shareService = shareService;
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

    private bool CanAddToList() => Product is not null && !IsSharing;

    [RelayCommand(CanExecute = nameof(CanAddToList))]
    private async Task AddToListAsync()
    {
        if (Product is null)
        {
            return;
        }

        try
        {
            IsSharing = true;
            ErrorMessage = null;

            string? city;
            try
            {
                city = await _locationService.GetCurrentCityAsync();
            }
            catch
            {
                city = null;
            }

            if (city is null)
            {
                ErrorMessage = "Couldn't determine your location, so \"your area\" was used instead.";
            }

            var shareText = $"{Product.Title} - {Product.Price} from {city ?? "your area"} added to list";

            await _shareService.ShareTextAsync(shareText, title: "Add to list");
        }
        finally
        {
            IsSharing = false;
        }
    }
}
