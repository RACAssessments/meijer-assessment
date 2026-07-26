namespace MeijerProducts.Services;

public interface INavigationService
{
    // Route strings are Shell routes, optionally with query parameters
    // (e.g. "productdetail?id=7") - see Helpers/Routes.cs.
    Task GoToAsync(string route);
}
