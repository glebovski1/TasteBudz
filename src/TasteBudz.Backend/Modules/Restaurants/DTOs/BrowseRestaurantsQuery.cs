using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

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

    // Raised from 100 to 2000 to support loading the full local restaurant
    // catalog onto the event creation map without pagination.
    [Range(1, 2000)]
    public int PageSize { get; init; } = 20;
}