namespace TasteBudz.Backend.Modules.Restaurants;

public sealed record DiscountActivationDto(
    Guid ReservationId,
    bool IsActive,
    bool IsFinalized,
    int JoinedParticipantCount,
    int MinThresholdForDiscount,
    DateTimeOffset EvaluatedAtUtc);
