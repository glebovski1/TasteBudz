using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class RestaurantIndexViewModel
{
    public const int PageSize = 50;

    public IReadOnlyCollection<RestaurantDto> Restaurants { get; init; } = [];
    public int CurrentPage { get; init; } = 1;
    public int TotalCount { get; init; }
    public string? SearchQuery { get; init; }
    public string? Cuisine { get; init; }
    public PriceTier? PriceTier { get; init; }
    public string? ZipCode { get; init; }
    public double? RadiusMiles { get; init; }
    public IReadOnlyCollection<string> SuggestedCuisineTags { get; init; } = [];

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(SearchQuery) ||
        !string.IsNullOrWhiteSpace(Cuisine) ||
        PriceTier.HasValue ||
        !string.IsNullOrWhiteSpace(ZipCode) ||
        RadiusMiles.HasValue;
}
