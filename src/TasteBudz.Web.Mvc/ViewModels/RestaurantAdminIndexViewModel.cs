using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class RestaurantAdminIndexViewModel
{
    public bool OperationsAvailable { get; init; } = true;
    public IReadOnlyCollection<RestaurantDto> Restaurants { get; init; } = [];
}
