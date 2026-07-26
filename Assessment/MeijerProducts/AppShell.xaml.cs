using MeijerProducts.Helpers;
using MeijerProducts.Views;

namespace MeijerProducts
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(Routes.ProductDetail, typeof(ProductDetailPage));
        }
    }
}
