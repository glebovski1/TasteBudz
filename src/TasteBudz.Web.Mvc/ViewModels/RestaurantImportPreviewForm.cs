using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public class RestaurantImportPreviewForm
{
    [MaxLength(40)]
    public string? Preset { get; set; } = "cincinnati";

    [RegularExpression("^[0-9]{5}$")]
    public string? ZipCode { get; set; } = "45202";

    [Range(1, 50)]
    public double? RadiusMiles { get; set; } = 25;

    public double? South { get; set; }
    public double? West { get; set; }
    public double? North { get; set; }
    public double? East { get; set; }

    public RestaurantImportPreviewQuery ToQuery() => new()
    {
        Preset = Preset,
        ZipCode = ZipCode,
        RadiusMiles = RadiusMiles,
        South = South,
        West = West,
        North = North,
        East = East,
    };
}
