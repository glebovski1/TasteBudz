using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

public sealed record EventSummaryDto(
    Guid EventId,
    string? Title,
    EventType EventType,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc,
    DateTimeOffset DecisionAtUtc,
    int Capacity,
    int ActiveParticipants,
    Guid HostUserId,
    Guid? SelectedRestaurantId,
    string? CuisineTarget,
    Guid? GroupId,
    double? DistanceMiles = null,
    int MatchingCuisineCount = 0,
    int MatchingBudzCount = 0,
    bool HasActiveSlotReservation = false,
    bool IsDiscountActive = false,
    int? DiscountPercent = null);
