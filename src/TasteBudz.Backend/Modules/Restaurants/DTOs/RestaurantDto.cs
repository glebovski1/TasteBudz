using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed record RestaurantDto(
    Guid RestaurantId,
    string Name,
    string City,
    string State,
    string ZipCode,
    PriceTier PriceTier,
    IReadOnlyCollection<string> CuisineTags,
    double? Latitude,
    double? Longitude,
    string? ExternalPlaceId,
    double? DistanceMiles);
