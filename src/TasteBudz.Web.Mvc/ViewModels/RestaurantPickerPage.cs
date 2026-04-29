using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class RestaurantPickerPage
{
    public static RestaurantPickerPage Empty { get; } = new();

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = EventCreateViewModel.RestaurantPickerPageSize;
    public int TotalCount { get; init; }
    public IReadOnlyList<RestaurantPickerItem> Restaurants { get; init; } = [];
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
