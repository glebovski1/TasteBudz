using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class AdminRestaurantsViewModel
{
    public const int PageSize = 25;

    public AdminRestaurantCatalogForm CreateForm { get; init; } = new();
    public IReadOnlyCollection<AdminRestaurantCatalogItemViewModel> Restaurants { get; init; } = [];
    public IReadOnlyDictionary<Guid, IReadOnlyCollection<RestaurantAdminAssignmentDto>> AssignmentsByRestaurantId { get; init; } =
        new Dictionary<Guid, IReadOnlyCollection<RestaurantAdminAssignmentDto>>();
    public IReadOnlyCollection<string> SuggestedCuisineTags { get; init; } = [];
    public string? Q { get; init; }
    public AdminRestaurantCatalogStatus? FilterStatus { get; init; }
    public AdminRestaurantCatalogSource? FilterSource { get; init; }
    public int CurrentPage { get; init; } = 1;
    public int TotalCount { get; init; }
    public Guid? EditRestaurantId { get; init; }
    public RestaurantImportPreviewForm ImportForm { get; init; } = new();
    public RestaurantImportPreviewDto? ImportPreview { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public AdminRestaurantCatalogItemViewModel? EditItem => EditRestaurantId.HasValue
        ? Restaurants.FirstOrDefault(item => item.Restaurant.RestaurantId == EditRestaurantId.Value)
        : null;
}
