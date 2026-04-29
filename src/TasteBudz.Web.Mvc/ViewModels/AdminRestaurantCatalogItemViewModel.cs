using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class AdminRestaurantCatalogItemViewModel
{
    public AdminRestaurantCatalogItemDto Restaurant { get; init; } = null!;
    public AdminRestaurantCatalogForm Form { get; init; } = new();
}
