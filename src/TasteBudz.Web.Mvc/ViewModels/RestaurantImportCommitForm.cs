using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class RestaurantImportCommitForm : RestaurantImportPreviewForm
{
    public List<string> SelectedExternalPlaceIds { get; set; } = [];

    public CommitRestaurantImportRequest ToRequest() => new()
    {
        Preset = Preset,
        ZipCode = ZipCode,
        RadiusMiles = RadiusMiles,
        South = South,
        West = West,
        North = North,
        East = East,
        SelectedExternalPlaceIds = SelectedExternalPlaceIds,
    };
}
