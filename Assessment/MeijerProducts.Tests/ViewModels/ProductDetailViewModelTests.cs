using MeijerProducts.Models;
using MeijerProducts.Services;
using MeijerProducts.ViewModels;
using Moq;
using Xunit;

namespace MeijerProducts.Tests.ViewModels;

public class ProductDetailViewModelTests
{
    private static ProductDetail CreateProduct() => new()
    {
        Id = 0,
        ImageUrl = "https://example.com/bananas.png",
        Summary = "Fresh bananas.",
        Title = "Bananas",
        Description = "Fresh bananas, perfect for a healthy snack.",
        Price = "$0.59/lb"
    };

    private static ProductDetailViewModel CreateViewModel(
        Mock<ILocationService> locationService,
        Mock<IShareService> shareService,
        ProductDetail? product = null)
    {
        var vm = new ProductDetailViewModel(
            Mock.Of<IProductService>(),
            locationService.Object,
            shareService.Object);

        if (product is not null)
        {
            vm.Product = product;
        }

        return vm;
    }

    [Fact]
    public async Task AddToListAsync_WhenCityResolves_BuildsExpectedShareText()
    {
        var locationService = new Mock<ILocationService>();
        locationService.Setup(s => s.GetCurrentCityAsync(default)).ReturnsAsync("Chicago");
        var shareService = new Mock<IShareService>();

        var vm = CreateViewModel(locationService, shareService, CreateProduct());

        await vm.AddToListCommand.ExecuteAsync(null);

        shareService.Verify(
            s => s.ShareTextAsync("Bananas - $0.59/lb from Chicago added to list", "Add to list"),
            Times.Once);
    }

    [Fact]
    public async Task AddToListAsync_WhenCityUnresolved_FallsBackToPlaceholderAndStillShares()
    {
        var locationService = new Mock<ILocationService>();
        locationService.Setup(s => s.GetCurrentCityAsync(default)).ReturnsAsync((string?)null);
        var shareService = new Mock<IShareService>();

        var vm = CreateViewModel(locationService, shareService, CreateProduct());

        await vm.AddToListCommand.ExecuteAsync(null);

        shareService.Verify(
            s => s.ShareTextAsync("Bananas - $0.59/lb from your area added to list", "Add to list"),
            Times.Once);
        Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));
    }

    [Fact]
    public async Task AddToListAsync_TogglesIsSharing_WhileCommandIsInFlight()
    {
        var citySource = new TaskCompletionSource<string?>();
        var locationService = new Mock<ILocationService>();
        locationService.Setup(s => s.GetCurrentCityAsync(default)).Returns(citySource.Task);
        var shareService = new Mock<IShareService>();

        var vm = CreateViewModel(locationService, shareService, CreateProduct());

        var executeTask = vm.AddToListCommand.ExecuteAsync(null);

        Assert.True(vm.IsSharing);

        citySource.SetResult("Chicago");
        await executeTask;

        Assert.False(vm.IsSharing);
    }

    [Fact]
    public async Task AddToListAsync_PassesPriceThrough_WithoutReformatting()
    {
        var locationService = new Mock<ILocationService>();
        locationService.Setup(s => s.GetCurrentCityAsync(default)).ReturnsAsync("Chicago");
        var shareService = new Mock<IShareService>();

        var product = CreateProduct();
        product.Title = "Whole Milk";
        product.Price = "$3.49/gal";

        var vm = CreateViewModel(locationService, shareService, product);

        await vm.AddToListCommand.ExecuteAsync(null);

        shareService.Verify(
            s => s.ShareTextAsync("Whole Milk - $3.49/gal from Chicago added to list", "Add to list"),
            Times.Once);
    }

    [Fact]
    public async Task AddToListAsync_WhenProductIsNull_DoesNotShare()
    {
        var locationService = new Mock<ILocationService>();
        var shareService = new Mock<IShareService>();

        var vm = CreateViewModel(locationService, shareService);

        await vm.AddToListCommand.ExecuteAsync(null);

        shareService.Verify(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
}
