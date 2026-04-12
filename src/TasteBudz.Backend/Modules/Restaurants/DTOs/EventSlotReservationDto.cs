using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed record EventSlotReservationDto(
    Guid ReservationId,
    Guid EventId,
    Guid SlotId,
    Guid RestaurantId,
    EventSlotReservationStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);
