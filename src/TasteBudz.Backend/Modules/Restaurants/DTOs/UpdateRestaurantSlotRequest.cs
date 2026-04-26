using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed class UpdateRestaurantSlotRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? StartsAtUtc { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? EndsAtUtc { get; init; }

    [Range(2, 8)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Capacity { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CutoffAtUtc { get; init; }

    [Range(2, 8)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinThresholdForDiscount { get; init; }

    [Range(1, 100)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DiscountPercent { get; init; }

    public bool ClearDiscount { get; init; }
}
