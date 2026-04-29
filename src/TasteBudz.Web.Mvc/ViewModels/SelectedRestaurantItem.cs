using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class SelectedRestaurantItem
{
    public Guid RestaurantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string PriceTier { get; init; } = string.Empty;
    public string CuisineTags { get; init; } = string.Empty;
    public string GoogleMapsUrl { get; init; } = string.Empty;

    public static SelectedRestaurantItem FromDto(RestaurantDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name,
        Location = $"{dto.City}, {dto.State} {dto.ZipCode}",
        PriceTier = new string('$', (int)dto.PriceTier + 1),
        CuisineTags = string.Join(", ", dto.CuisineTags),
        GoogleMapsUrl = RestaurantMapsLinkBuilder.BuildGoogleMapsUrl(dto),
    };
}
