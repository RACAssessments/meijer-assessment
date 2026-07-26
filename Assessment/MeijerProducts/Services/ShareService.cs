using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace MeijerProducts.Services;

public class ShareService : IShareService
{
    public Task ShareTextAsync(string text, string? title = null)
    {
        return Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = text,
            Title = title
        });
    }
}
