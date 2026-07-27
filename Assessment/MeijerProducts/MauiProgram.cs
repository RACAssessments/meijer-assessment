using MeijerProducts.Services;
using MeijerProducts.ViewModels;
using MeijerProducts.Views;
using Microsoft.Extensions.Logging;

namespace MeijerProducts
{
    public static class MauiProgram
    {
        private const string ProductionApiBaseAddress =
            "http://meijerproducts-api.southcentralus.azurecontainer.io:8080/";

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton(_ =>
            {
#if DEBUG
                // Android emulators route "localhost" to the emulator itself, not the host
                // machine running the API, so the loopback address needs to be host-mapped.
                var baseAddress = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:5217/"
                    : "http://localhost:5217/";
#else
                var baseAddress = ProductionApiBaseAddress;
#endif

                return new HttpClient { BaseAddress = new Uri(baseAddress) };
            });
            builder.Services.AddSingleton<IProductService, ProductService>();
            builder.Services.AddSingleton<ILocationService, LocationService>();
            builder.Services.AddSingleton<IShareService, ShareService>();
            builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

            builder.Services.AddTransient<ProductListViewModel>();
            builder.Services.AddTransient<ProductListPage>();
            builder.Services.AddTransient<ProductDetailViewModel>();
            builder.Services.AddTransient<ProductDetailPage>();

            return builder.Build();
        }
    }
}
