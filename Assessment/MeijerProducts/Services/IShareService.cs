namespace MeijerProducts.Services;

public interface IShareService
{
    Task ShareTextAsync(string text, string? title = null);
}
