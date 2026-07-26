using MeijerProducts.Models;
using MeijerProducts.Services;
using MeijerProducts.ViewModels;
using Moq;
using Xunit;

namespace MeijerProducts.Tests.ViewModels;

// Covers the load/navigation half of ProductDetailViewModel; the share half lives in
// ProductDetailViewModelTests.
public class ProductDetailViewModelLoadTests
{
    private const string ConnectionErrorMessage =
        "Unable to load product details. Please check your connection and try again.";

    private static ProductDetail CreateProduct(int id = 0) => new()
    {
        Id = id,
        ImageUrl = "https://example.com/bananas.png",
        Summary = "Fresh bananas.",
        Title = "Bananas",
        Description = "Fresh bananas, perfect for a healthy snack.",
        Price = "$0.59/lb"
    };

    private static ProductDetailViewModel CreateViewModel(Mock<IProductService> productService) =>
        new(productService.Object, Mock.Of<ILocationService>(), Mock.Of<IShareService>());

    [Fact]
    public async Task LoadProductAsync_WhenProductExists_SetsProductAndClearsError()
    {
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductAsync(7, default)).ReturnsAsync(CreateProduct(7));

        var vm = CreateViewModel(productService);

        await vm.LoadProductCommand.ExecuteAsync(7);

        Assert.NotNull(vm.Product);
        Assert.Equal(7, vm.Product.Id);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadProductAsync_WhenProductNotFound_SetsProductNotFoundMessage()
    {
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductAsync(9999, default)).ReturnsAsync((ProductDetail?)null);

        var vm = CreateViewModel(productService);

        await vm.LoadProductCommand.ExecuteAsync(9999);

        Assert.Null(vm.Product);
        Assert.Equal("Product not found.", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadProductAsync_WhenServiceThrowsHttpRequestException_SetsConnectionErrorMessage()
    {
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductAsync(0, default)).ThrowsAsync(new HttpRequestException());

        var vm = CreateViewModel(productService);

        await vm.LoadProductCommand.ExecuteAsync(0);

        Assert.Equal(ConnectionErrorMessage, vm.ErrorMessage);
        Assert.Null(vm.Product);
    }

    [Fact]
    public async Task LoadProductAsync_TogglesIsBusy_WhileLoadIsInFlight()
    {
        var productSource = new TaskCompletionSource<ProductDetail?>();
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductAsync(0, default)).Returns(productSource.Task);

        var vm = CreateViewModel(productService);

        var executeTask = vm.LoadProductCommand.ExecuteAsync(0);

        Assert.True(vm.IsBusy);

        productSource.SetResult(CreateProduct());
        await executeTask;

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task LoadProductAsync_WhenLoadFails_ResetsIsBusy()
    {
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductAsync(0, default)).ThrowsAsync(new HttpRequestException());

        var vm = CreateViewModel(productService);

        await vm.LoadProductCommand.ExecuteAsync(0);

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task LoadProductAsync_WhenProductLoads_EnablesAddToListCommand()
    {
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductAsync(0, default)).ReturnsAsync(CreateProduct());

        var vm = CreateViewModel(productService);

        Assert.False(vm.AddToListCommand.CanExecute(null));

        await vm.LoadProductCommand.ExecuteAsync(0);

        Assert.True(vm.AddToListCommand.CanExecute(null));
    }

    [Fact]
    public async Task ApplyQueryAttributes_WhenIdIsValid_RequestsThatProduct()
    {
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductAsync(7, default)).ReturnsAsync(CreateProduct(7));

        var vm = CreateViewModel(productService);

        // Shell hands query values over as strings, not boxed ints.
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "7" });

        // ApplyQueryAttributes is void and starts the load fire-and-forget, so wait on the
        // command's own task rather than racing it.
        await vm.LoadProductCommand.ExecutionTask!;

        productService.Verify(s => s.GetProductAsync(7, default), Times.Once);
        Assert.Equal(7, vm.Product?.Id);
    }

    [Fact]
    public void ApplyQueryAttributes_WhenIdIsMissing_DoesNotCallService()
    {
        var productService = new Mock<IProductService>();
        var vm = CreateViewModel(productService);

        vm.ApplyQueryAttributes(new Dictionary<string, object>());

        productService.Verify(
            s => s.GetProductAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void ApplyQueryAttributes_WhenIdIsNotAnInteger_DoesNotCallService()
    {
        var productService = new Mock<IProductService>();
        var vm = CreateViewModel(productService);

        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "abc" });

        productService.Verify(
            s => s.GetProductAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
