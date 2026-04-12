using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed class UpdateRestaurantSlotRequest
{
    public DateTimeOffset? StartsAtUtc { get; init; }

    public DateTimeOffset? EndsAtUtc { get; init; }

    [Range(2, 8)]
    public int? Capacity { get; init; }

    public DateTimeOffset? CutoffAtUtc { get; init; }

    [Range(2, 8)]
    public int? MinThresholdForDiscount { get; init; }
}
