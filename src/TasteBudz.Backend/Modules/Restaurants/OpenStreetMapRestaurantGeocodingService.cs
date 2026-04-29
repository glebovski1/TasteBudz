using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Extensions;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Resolves manual admin-entered restaurant addresses into persisted coordinates.
/// </summary>
public sealed class OpenStreetMapRestaurantGeocodingService(
    IHttpClientFactory httpClientFactory,
    ILogger<OpenStreetMapRestaurantGeocodingService> logger) : IRestaurantGeocodingService
{
    public async Task<RestaurantGeocodeResult?> GeocodeAsync(
        string restaurantName,
        string? streetAddress,
        string city,
        string state,
        string zipCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Nominatim");
            var path = BuildSearchPath(restaurantName, streetAddress, city, state, zipCode);
            var results = await client.GetFromJsonAsync<NominatimSearchResult[]>(path, cancellationToken);
            var match = results?.FirstOrDefault();

            if (match is null ||
                !double.TryParse(match.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(match.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                return null;
            }

            return new RestaurantGeocodeResult(
                latitude,
                longitude,
                BuildExternalPlaceId(match.OsmType, match.OsmId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Restaurant geocoding failed for {RestaurantName} in {ZipCode}.", restaurantName, zipCode);
            return null;
        }
    }

    private static string BuildSearchPath(string restaurantName, string? streetAddress, string city, string state, string zipCode)
    {
        var builder = new QueryBuilder
        {
            { "format", "jsonv2" },
            { "limit", "1" },
            { "countrycodes", "us" },
        };

        if (!string.IsNullOrWhiteSpace(streetAddress))
        {
            builder.Add("amenity", restaurantName);
            builder.Add("street", streetAddress);
            builder.Add("city", city);
            builder.Add("state", state);
            builder.Add("postalcode", zipCode);
        }
        else
        {
            builder.Add("q", $"{restaurantName}, {city}, {state} {zipCode}");
        }

        return $"/search{builder.ToQueryString()}";
    }

    private static string? BuildExternalPlaceId(string? osmType, long? osmId)
    {
        if (string.IsNullOrWhiteSpace(osmType) || !osmId.HasValue)
        {
            return null;
        }

        return $"osm:{osmType.Trim()}:{osmId.Value}";
    }

}
