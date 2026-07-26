namespace MeijerProducts.Services;

public interface ILocationService
{
    // Never throws - returns null on permission denial, disabled location, no fix,
    // timeout, or a failed/empty reverse-geocode, so callers can fall back to a placeholder.
    Task<string?> GetCurrentCityAsync(CancellationToken cancellationToken = default);
}
