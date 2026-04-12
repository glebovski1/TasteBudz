using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed class UpdateManagedRestaurantRequest
{
    [MaxLength(160)]
    public string? Name { get; init; }

    [MaxLength(80)]
    public string? City { get; init; }

    [MaxLength(2)]
    public string? State { get; init; }

    [MaxLength(10)]
    public string? ZipCode { get; init; }

    public PriceTier? PriceTier { get; init; }

    [MaxLength(160)]
    public string? ExternalPlaceId { get; init; }
}
