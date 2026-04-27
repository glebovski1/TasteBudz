using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed class AdminRestaurantSearchQuery
{
    public string? Q { get; init; }

    public AdminRestaurantCatalogStatus? Status { get; init; }

    public AdminRestaurantCatalogSource? Source { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;
}

public enum AdminRestaurantCatalogStatus
{
    All = 0,
    Active = 1,
    Archived = 2,
}

public enum AdminRestaurantCatalogSource
{
    All = 0,
    Manual = 1,
    OpenStreetMap = 2,
}
