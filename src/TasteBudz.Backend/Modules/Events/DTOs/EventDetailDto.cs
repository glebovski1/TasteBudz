using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Modules.Events;

public sealed record EventDetailDto(
    Guid EventId,
    string? Title,
    EventType EventType,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc,
    DateTimeOffset DecisionAtUtc,
    int Capacity,
    int MinParticipantsToRun,
    int ActiveParticipants,
    Guid HostUserId,
    Guid? SelectedRestaurantId,
    string? CuisineTarget,
    Guid? GroupId,
    string? CancellationReason,
    EventSlotReservationDto? SlotReservation = null,
    DiscountActivationDto? DiscountActivation = null);
