using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


internal static class RestaurantMapsLinkBuilder
{
    private const string GooglePlaceIdPrefix = "google:";
    private const string OpenStreetMapPlaceIdPrefix = "osm:";

    public static string BuildGoogleMapsUrl(RestaurantDto restaurant)
    {
        var query = string.IsNullOrWhiteSpace(restaurant.StreetAddress)
            ? $"{restaurant.Name}, {restaurant.City}, {restaurant.State} {restaurant.ZipCode}".Trim()
            : $"{restaurant.Name}, {restaurant.StreetAddress}, {restaurant.City}, {restaurant.State} {restaurant.ZipCode}".Trim();
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://www.google.com/maps/search/?api=1&query={encodedQuery}";

        var externalPlaceId = restaurant.ExternalPlaceId?.Trim();
        if (string.IsNullOrWhiteSpace(externalPlaceId) ||
            externalPlaceId.StartsWith(OpenStreetMapPlaceIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (externalPlaceId.StartsWith(GooglePlaceIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            externalPlaceId = externalPlaceId[GooglePlaceIdPrefix.Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(externalPlaceId) ||
            externalPlaceId.Contains(':', StringComparison.Ordinal))
        {
            return url;
        }

        return $"{url}&query_place_id={Uri.EscapeDataString(externalPlaceId)}";
    }
}
