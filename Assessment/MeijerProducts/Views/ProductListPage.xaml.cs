using MeijerProducts.ViewModels;

namespace MeijerProducts.Views;

public partial class ProductListPage : ContentPage
{
    private readonly ProductListViewModel _viewModel;

    public ProductListPage(ProductListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.Products.Count == 0)
        {
            _viewModel.LoadProductsCommand.Execute(null);
        }
    }
}
