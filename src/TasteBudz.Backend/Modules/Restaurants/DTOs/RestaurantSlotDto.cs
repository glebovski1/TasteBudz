using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed record RestaurantSlotDto(
    Guid SlotId,
    Guid RestaurantId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    int Capacity,
    DateTimeOffset CutoffAtUtc,
    int? MinThresholdForDiscount,
    int? DiscountPercent,
    RestaurantSlotStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);
