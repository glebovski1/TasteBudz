using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed class SaveRestaurantCatalogRequest
{
    [Required]
    [MaxLength(160)]
    public string? Name { get; init; }

    [MaxLength(160)]
    public string? StreetAddress { get; init; }

    [Required]
    [MaxLength(80)]
    public string? City { get; init; }

    [Required]
    [MaxLength(2)]
    public string? State { get; init; }

    [Required]
    [MaxLength(10)]
    public string? ZipCode { get; init; }

    [Required]
    public PriceTier? PriceTier { get; init; }

    public IReadOnlyCollection<string> CuisineTags { get; init; } = [];
}
