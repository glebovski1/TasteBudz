using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;

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
