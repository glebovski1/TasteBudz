// Request and response contracts for the restaurant catalog endpoints.
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Restaurant card returned by browse, detail, and suggestion endpoints.
/// </summary>
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

/// <summary>
/// Query parameters for restaurant search.
/// </summary>
public sealed class BrowseRestaurantsQuery
{
    public string? Q { get; init; }

    public string? Cuisine { get; init; }

    public PriceTier? PriceTier { get; init; }

    [RegularExpression("^[0-9]{5}$")]
    public string? ZipCode { get; init; }

    [Range(1, 100)]
    public double? RadiusMiles { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Query parameters for restaurant suggestions, optionally seeded from an event.
/// </summary>
public sealed class RestaurantSuggestionsQuery
{
    public Guid? EventId { get; init; }

    public Guid? GroupId { get; init; }

    [RegularExpression("^[0-9]{5}$")]
    public string? ZipCode { get; init; }

    [Range(1, 100)]
    public double? RadiusMiles { get; init; }

    public IReadOnlyCollection<string> CuisineTags { get; init; } = Array.Empty<string>();
}