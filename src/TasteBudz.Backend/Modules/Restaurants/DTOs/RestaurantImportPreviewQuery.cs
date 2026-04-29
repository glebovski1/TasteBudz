using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;


public class RestaurantImportPreviewQuery
{
    public string? Preset { get; init; } = "cincinnati";

    [RegularExpression("^[0-9]{5}$")]
    public string? ZipCode { get; init; } = "45202";

    [Range(1, 50)]
    public double? RadiusMiles { get; init; } = 25;

    public double? South { get; init; }

    public double? West { get; init; }

    public double? North { get; init; }

    public double? East { get; init; }
}
