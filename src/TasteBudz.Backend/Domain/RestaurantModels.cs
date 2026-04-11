// Restaurant catalog records used by browse and recommendation flows.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// Catalog entry for a restaurant that can be searched or attached to events.
/// </summary>
public sealed record Restaurant(
    Guid Id,
    string Name,
    string City,
    string State,
    string ZipCode,
    double? Latitude,
    double? Longitude,
    PriceTier PriceTier,
    IReadOnlyCollection<string> CuisineTags,
    string? ExternalPlaceId);

/// <summary>
/// Active or revoked management link between a user account and a restaurant.
/// </summary>
public sealed record RestaurantAdminAssignment(
    Guid RestaurantId,
    Guid UserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? RevokedAtUtc);

/// <summary>
/// Restaurant-owned availability window that can be reserved by one event.
/// </summary>
public sealed record RestaurantSlot(
    Guid Id,
    Guid RestaurantId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    int Capacity,
    DateTimeOffset CutoffAtUtc,
    int? MinThresholdForDiscount,
    RestaurantSlotStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);

/// <summary>
/// Event-to-slot link created by an event host.
/// </summary>
public sealed record EventSlotReservation(
    Guid Id,
    Guid EventId,
    Guid SlotId,
    EventSlotReservationStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);

/// <summary>
/// Persisted simulation result for discount eligibility on a slot-linked event.
/// </summary>
public sealed record DiscountActivation(
    Guid ReservationId,
    bool IsActive,
    bool IsFinalized,
    DateTimeOffset EvaluatedAtUtc);
