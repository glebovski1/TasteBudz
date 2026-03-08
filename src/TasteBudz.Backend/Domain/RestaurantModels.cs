// Restaurant catalog records used by browse and recommendation flows.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// Catalog entry for a restaurant that can be searched or attached to events.
/// </summary>
public sealed record Restaurant(
    Guid Id,
    string Name,
    string City,
    string State,
    string ZipCode,
    double? Latitude,
    double? Longitude,
    PriceTier PriceTier,
    IReadOnlyCollection<string> CuisineTags,
    string? ExternalPlaceId);