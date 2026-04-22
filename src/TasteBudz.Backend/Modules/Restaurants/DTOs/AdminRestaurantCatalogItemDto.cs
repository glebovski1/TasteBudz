using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed record AdminRestaurantCatalogItemDto(
    Guid RestaurantId,
    string Name,
    string? StreetAddress,
    string City,
    string State,
    string ZipCode,
    PriceTier PriceTier,
    IReadOnlyCollection<string> CuisineTags,
    double? Latitude,
    double? Longitude,
    string? ExternalPlaceId,
    bool IsArchived);
