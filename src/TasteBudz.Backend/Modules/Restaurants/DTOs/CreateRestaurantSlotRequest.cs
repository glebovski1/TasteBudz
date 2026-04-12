using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed class CreateRestaurantSlotRequest
{
    [Required]
    public DateTimeOffset? StartsAtUtc { get; init; }

    [Required]
    public DateTimeOffset? EndsAtUtc { get; init; }

    [Required]
    [Range(2, 8)]
    public int? Capacity { get; init; }

    [Required]
    public DateTimeOffset? CutoffAtUtc { get; init; }

    [Range(2, 8)]
    public int? MinThresholdForDiscount { get; init; }
}
