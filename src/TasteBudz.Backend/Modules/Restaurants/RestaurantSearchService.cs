// Search and distance-filter logic for the seeded restaurant catalog.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.ProblemDetails;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Builds the restaurant browse response from the in-memory catalog.
/// </summary>
public sealed class RestaurantSearchService(IRestaurantRepository restaurantRepository)
{
    public async Task<ListResponse<RestaurantDto>> BrowseAsync(BrowseRestaurantsQuery query, CancellationToken cancellationToken = default)
    {
        var restaurants = await restaurantRepository.ListAsync(cancellationToken);
        var referencePoint = await ResolveReferencePointAsync(query.ZipCode, cancellationToken);
        var filtered = ApplyFilters(restaurants, query.Q, query.Cuisine, query.PriceTier, referencePoint, query.RadiusMiles)
            .Select(restaurant => ToDto(restaurant, referencePoint))
            .OrderBy(restaurant => restaurant.DistanceMiles ?? double.MaxValue)
            .ThenBy(restaurant => restaurant.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var items = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new ListResponse<RestaurantDto>(items, filtered.Length);
    }

    public async Task<RestaurantDto> GetAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        var restaurant = await restaurantRepository.GetAsync(restaurantId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restaurant could not be found.");

        return ToDto(restaurant, null);
    }

    internal async Task<(double Latitude, double Longitude)?> ResolveReferencePointAsync(string? zipCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
        {
            return null;
        }

        return await restaurantRepository.GetZipCoordinatesAsync(zipCode.Trim(), cancellationToken);
    }

    internal static IEnumerable<Restaurant> ApplyFilters(
        IEnumerable<Restaurant> restaurants,
        string? query,
        string? cuisine,
        PriceTier? priceTier,
        (double Latitude, double Longitude)? referencePoint,
        double? radiusMiles)
    {
        var result = restaurants;

        if (!string.IsNullOrWhiteSpace(query))
        {
            result = result.Where(restaurant =>
                restaurant.Name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(cuisine))
        {
            result = result.Where(restaurant =>
                restaurant.CuisineTags.Any(tag => string.Equals(tag, cuisine.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        if (priceTier.HasValue)
        {
            result = result.Where(restaurant => restaurant.PriceTier == priceTier.Value);
        }

        if (referencePoint.HasValue && radiusMiles.HasValue)
        {
            result = result.Where(restaurant =>
                restaurant.Latitude.HasValue &&
                restaurant.Longitude.HasValue &&
                CalculateDistanceMiles(referencePoint.Value.Latitude, referencePoint.Value.Longitude, restaurant.Latitude.Value, restaurant.Longitude.Value) <= radiusMiles.Value);
        }

        return result;
    }

    internal static RestaurantDto ToDto(Restaurant restaurant, (double Latitude, double Longitude)? referencePoint)
    {
        double? distance = null;

        if (referencePoint.HasValue && restaurant.Latitude.HasValue && restaurant.Longitude.HasValue)
        {
            distance = Math.Round(CalculateDistanceMiles(referencePoint.Value.Latitude, referencePoint.Value.Longitude, restaurant.Latitude.Value, restaurant.Longitude.Value), 2);
        }

        return new RestaurantDto(
            restaurant.Id,
            restaurant.Name,
            restaurant.City,
            restaurant.State,
            restaurant.ZipCode,
            restaurant.PriceTier,
            restaurant.CuisineTags,
            restaurant.Latitude,
            restaurant.Longitude,
            restaurant.ExternalPlaceId,
            distance);
    }

    internal static double CalculateDistanceMiles(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMiles = 3958.8;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var startLat = DegreesToRadians(lat1);
        var endLat = DegreesToRadians(lat2);

        // Haversine distance is accurate enough for the seeded local-search use case.
        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(startLat) * Math.Cos(endLat) * Math.Pow(Math.Sin(dLon / 2), 2);
        var c = 2 * Math.Asin(Math.Sqrt(a));

        return earthRadiusMiles * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}