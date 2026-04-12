using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed class CancelRestaurantSlotRequest
{
    [Required]
    [MaxLength(250)]
    public string? Reason { get; init; }
}
