using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class RestaurantAdminManageViewModel
{
    public const int SlotPageSize = 10;

    public RestaurantDto Restaurant { get; init; } = null!;
    public IReadOnlyCollection<RestaurantSlotDto> Slots { get; init; } = [];
    public IReadOnlyCollection<RestaurantSlotDto> VisibleSlots { get; init; } = [];
    public RestaurantSlotDto? EditSlot { get; init; }
    public ManagedRestaurantForm RestaurantForm { get; init; } = new();
    public RestaurantSlotForm SlotForm { get; init; } = new();
    public RestaurantSlotEditForm? EditSlotForm { get; init; }
    public int SlotPage { get; init; } = 1;
    public int SlotTotalCount { get; init; }
    public int SlotTotalPages => Math.Max(1, (int)Math.Ceiling(SlotTotalCount / (double)SlotPageSize));
    public RestaurantSlotStatus? SlotStatus { get; init; }
}
