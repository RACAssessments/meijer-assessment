using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace MeijerProducts.Services;

public class LocationService : ILocationService
{
    public async Task<string?> GetCurrentCityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                return null;
            }

            var location = await Geolocation.Default.GetLastKnownLocationAsync()
                ?? await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)),
                    cancellationToken);

            if (location is null)
            {
                return null;
            }

            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location);
            var city = placemarks?.FirstOrDefault()?.Locality;

            return string.IsNullOrWhiteSpace(city) ? null : city;
        }
        catch (Exception ex) when (
            ex is FeatureNotSupportedException or
            FeatureNotEnabledException or
            PermissionException or
            TaskCanceledException or
            OperationCanceledException)
        {
            return null;
        }
    }
}
