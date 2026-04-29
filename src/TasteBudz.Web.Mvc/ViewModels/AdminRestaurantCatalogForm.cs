using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class AdminRestaurantCatalogForm
{
    public Guid RestaurantId { get; init; }

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? StreetAddress { get; set; }

    [Required]
    [MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string ZipCode { get; set; } = string.Empty;

    public PriceTier PriceTier { get; set; }

    [Required]
    public string CuisineTagsText { get; set; } = string.Empty;

    public SaveRestaurantCatalogRequest ToRequest() => new()
    {
        Name = Name,
        StreetAddress = StreetAddress,
        City = City,
        State = State,
        ZipCode = ZipCode,
        PriceTier = PriceTier,
        CuisineTags = CuisineTagsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray(),
    };

    public static AdminRestaurantCatalogForm FromDto(AdminRestaurantCatalogItemDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name,
        StreetAddress = dto.StreetAddress,
        City = dto.City,
        State = dto.State,
        ZipCode = dto.ZipCode,
        PriceTier = dto.PriceTier,
        CuisineTagsText = string.Join(", ", dto.CuisineTags),
    };
}
