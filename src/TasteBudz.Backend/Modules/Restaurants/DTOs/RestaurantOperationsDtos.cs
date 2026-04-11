using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed record RestaurantAdminAssignmentDto(
    Guid RestaurantId,
    Guid UserId,
    string Username,
    DateTimeOffset CreatedAtUtc);

public sealed class CreateRestaurantAdminAssignmentRequest
{
    [Required]
    [MaxLength(80)]
    public string? Username { get; init; }
}

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

public sealed record RestaurantSlotDto(
    Guid SlotId,
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

public sealed class CancelRestaurantSlotRequest
{
    [Required]
    [MaxLength(250)]
    public string? Reason { get; init; }
}

public sealed class ReserveEventSlotRequest
{
    [Required]
    public Guid? SlotId { get; init; }
}

public sealed record EventSlotReservationDto(
    Guid ReservationId,
    Guid EventId,
    Guid SlotId,
    Guid RestaurantId,
    EventSlotReservationStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);

public sealed record DiscountActivationDto(
    Guid ReservationId,
    bool IsActive,
    bool IsFinalized,
    int JoinedParticipantCount,
    int MinThresholdForDiscount,
    DateTimeOffset EvaluatedAtUtc);
