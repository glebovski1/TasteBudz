using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;


public sealed class CommitRestaurantImportRequest : RestaurantImportPreviewQuery
{
    public IReadOnlyCollection<string> SelectedExternalPlaceIds { get; init; } = [];
}
