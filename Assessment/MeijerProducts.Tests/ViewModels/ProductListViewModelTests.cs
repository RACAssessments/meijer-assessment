using MeijerProducts.Models;
using MeijerProducts.Services;
using MeijerProducts.ViewModels;
using Moq;
using Xunit;

namespace MeijerProducts.Tests.ViewModels;

public class ProductListViewModelTests
{
    private const string ConnectionErrorMessage =
        "Unable to load products. Please check your connection and try again.";

    private static ProductSummary CreateSummary(int id = 0, string title = "Bananas") => new()
    {
        Id = id,
        ImageUrl = "https://example.com/bananas.png",
        Summary = "Fresh bananas.",
        Title = title
    };

    private static ProductListViewModel CreateViewModel(
        Mock<IProductService>? productService = null,
        Mock<INavigationService>? navigationService = null) =>
        new(
            productService?.Object ?? Mock.Of<IProductService>(),
            navigationService?.Object ?? Mock.Of<INavigationService>());

    [Fact]
    public async Task LoadProductsAsync_WhenServiceReturnsProducts_PopulatesCollection()
    {
        var productService = new Mock<IProductService>();
        productService
            .Setup(s => s.GetProductsAsync(default))
            .ReturnsAsync([CreateSummary(0, "Bananas"), CreateSummary(1, "Gala Apples")]);

        var vm = CreateViewModel(productService);

        await vm.LoadProductsCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Products.Count);
        Assert.Equal("Bananas", vm.Products[0].Title);
        Assert.Equal("Gala Apples", vm.Products[1].Title);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadProductsAsync_WhenCalledTwice_ReplacesRatherThanAppends()
    {
        var productService = new Mock<IProductService>();
        productService
            .SetupSequence(s => s.GetProductsAsync(default))
            .ReturnsAsync([CreateSummary(0), CreateSummary(1)])
            .ReturnsAsync([CreateSummary(2)]);

        var vm = CreateViewModel(productService);

        await vm.LoadProductsCommand.ExecuteAsync(null);
        await vm.LoadProductsCommand.ExecuteAsync(null);

        Assert.Single(vm.Products);
        Assert.Equal(2, vm.Products[0].Id);
    }

    [Fact]
    public async Task LoadProductsAsync_WhenServiceThrowsHttpRequestException_SetsUserFacingErrorMessage()
    {
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductsAsync(default)).ThrowsAsync(new HttpRequestException());

        var vm = CreateViewModel(productService);

        await vm.LoadProductsCommand.ExecuteAsync(null);

        Assert.Equal(ConnectionErrorMessage, vm.ErrorMessage);
        Assert.Empty(vm.Products);
    }

    [Fact]
    public async Task LoadProductsAsync_WhenServiceThrowsHttpRequestException_ResetsIsBusy()
    {
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductsAsync(default)).ThrowsAsync(new HttpRequestException());

        var vm = CreateViewModel(productService);

        await vm.LoadProductsCommand.ExecuteAsync(null);

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task LoadProductsAsync_OnSuccessfulReload_ClearsPreviousErrorMessage()
    {
        var productService = new Mock<IProductService>();
        productService
            .SetupSequence(s => s.GetProductsAsync(default))
            .ThrowsAsync(new HttpRequestException())
            .ReturnsAsync([CreateSummary()]);

        var vm = CreateViewModel(productService);

        await vm.LoadProductsCommand.ExecuteAsync(null);
        Assert.Equal(ConnectionErrorMessage, vm.ErrorMessage);

        await vm.LoadProductsCommand.ExecuteAsync(null);

        Assert.Null(vm.ErrorMessage);
        Assert.Single(vm.Products);
    }

    [Fact]
    public async Task LoadProductsAsync_TogglesIsBusy_WhileCommandIsInFlight()
    {
        var productsSource = new TaskCompletionSource<List<ProductSummary>>();
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductsAsync(default)).Returns(productsSource.Task);

        var vm = CreateViewModel(productService);

        var executeTask = vm.LoadProductsCommand.ExecuteAsync(null);

        Assert.True(vm.IsBusy);

        productsSource.SetResult([CreateSummary()]);
        await executeTask;

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task LoadProductsAsync_WhenAlreadyBusy_DoesNotCallService()
    {
        var productService = new Mock<IProductService>();
        var vm = CreateViewModel(productService);

        // Simulates a load already in flight - the guard is what stops a pull-to-refresh
        // landing on top of the initial page load.
        vm.IsBusy = true;

        await vm.LoadProductsCommand.ExecuteAsync(null);

        productService.Verify(s => s.GetProductsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadProductsAsync_WhenServiceThrowsUnexpectedException_Propagates()
    {
        // The catch is deliberately narrowed to HttpRequestException; anything else is a bug
        // that should surface rather than be shown to the user as a connection problem.
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductsAsync(default)).ThrowsAsync(new InvalidOperationException());

        var vm = CreateViewModel(productService);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => vm.LoadProductsCommand.ExecuteAsync(null));

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task GoToDetailsAsync_WhenProductIsSelected_NavigatesToDetailRouteWithId()
    {
        var navigationService = new Mock<INavigationService>();
        var vm = CreateViewModel(navigationService: navigationService);

        await vm.GoToDetailsCommand.ExecuteAsync(CreateSummary(7));

        // Pins the exact query-string shape ProductDetailViewModel.ApplyQueryAttributes parses.
        navigationService.Verify(n => n.GoToAsync("productdetail?id=7"), Times.Once);
    }

    [Fact]
    public async Task GoToDetailsAsync_WhenProductIsNull_DoesNotNavigate()
    {
        var navigationService = new Mock<INavigationService>();
        var vm = CreateViewModel(navigationService: navigationService);

        await vm.GoToDetailsCommand.ExecuteAsync(null);

        navigationService.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }
}
