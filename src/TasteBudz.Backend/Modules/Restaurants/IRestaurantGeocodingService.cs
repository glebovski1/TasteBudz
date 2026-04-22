namespace TasteBudz.Backend.Modules.Restaurants;

public interface IRestaurantGeocodingService
{
    Task<RestaurantGeocodeResult?> GeocodeAsync(
        string restaurantName,
        string? streetAddress,
        string city,
        string state,
        string zipCode,
        CancellationToken cancellationToken = default);
}

public sealed record RestaurantGeocodeResult(
    double Latitude,
    double Longitude,
    string? ExternalPlaceId);
