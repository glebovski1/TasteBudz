using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class RestaurantPickerItem
{
    public Guid RestaurantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string PriceTier { get; init; } = string.Empty;
    public string CuisineTags { get; init; } = string.Empty;
    public IReadOnlyList<string> CuisineTagList { get; init; } = [];
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string GoogleMapsUrl { get; init; } = string.Empty;
    public IReadOnlyList<RestaurantPickerSlotItem> AvailableSlots { get; init; } = [];
    public IReadOnlyList<RestaurantPickerSlotItem> NextMonthDiscountSlots { get; init; } = [];

    public static RestaurantPickerItem FromDto(
        RestaurantDto dto,
        IReadOnlyCollection<RestaurantSlotDto>? availableSlots = null,
        IReadOnlyCollection<RestaurantSlotDto>? nextMonthDiscountSlots = null) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name,
        Location = $"{dto.City}, {dto.State}",
        PriceTier = new string('$', (int)dto.PriceTier + 1),
        CuisineTags = string.Join(", ", dto.CuisineTags),
        CuisineTagList = dto.CuisineTags.ToArray(),
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        GoogleMapsUrl = RestaurantMapsLinkBuilder.BuildGoogleMapsUrl(dto),
        AvailableSlots = (availableSlots ?? Array.Empty<RestaurantSlotDto>())
            .OrderBy(slot => slot.StartsAtUtc)
            .Select(RestaurantPickerSlotItem.FromDto)
            .ToList(),
        NextMonthDiscountSlots = (nextMonthDiscountSlots ?? Array.Empty<RestaurantSlotDto>())
            .OrderBy(slot => slot.StartsAtUtc)
            .Select(RestaurantPickerSlotItem.FromDto)
            .ToList(),
    };
}
