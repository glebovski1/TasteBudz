using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class RestaurantAssignmentPanelItem
{
    public RestaurantDto Restaurant { get; init; } = null!;
    public IReadOnlyCollection<RestaurantAdminAssignmentDto> Assignments { get; init; } = [];
}
