// Internal mapping helpers that convert event domain records into API contracts.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Keeps controller and service code focused on workflow rules instead of DTO projection details.
/// </summary>
internal static class EventDtoMapper
{
    internal static EventSummaryDto ToSummary(Event eventRecord, int activeParticipants) =>
        new(
            eventRecord.Id,
            eventRecord.Title,
            eventRecord.EventType,
            eventRecord.Status,
            eventRecord.EventStartAtUtc,
            eventRecord.DecisionAtUtc,
            eventRecord.Capacity,
            activeParticipants,
            eventRecord.HostUserId,
            eventRecord.SelectedRestaurantId,
            eventRecord.CuisineTarget,
            eventRecord.GroupId);

    internal static EventDetailDto ToDetail(
        Event eventRecord,
        int activeParticipants,
        EventSlotReservationDto? slotReservation = null,
        DiscountActivationDto? discountActivation = null) =>
        new(
            eventRecord.Id,
            eventRecord.Title,
            eventRecord.EventType,
            eventRecord.Status,
            eventRecord.EventStartAtUtc,
            eventRecord.DecisionAtUtc,
            eventRecord.Capacity,
            eventRecord.MinParticipantsToRun,
            activeParticipants,
            eventRecord.HostUserId,
            eventRecord.SelectedRestaurantId,
            eventRecord.CuisineTarget,
            eventRecord.GroupId,
            eventRecord.CancellationReason,
            slotReservation,
            discountActivation);

    internal static EventParticipantDto ToParticipant(EventParticipant participant, UserAccount account, UserProfile? profile) =>
        new(
            participant.UserId,
            account.Username,
            profile?.DisplayName ?? account.Username,
            participant.State,
            participant.InvitedAtUtc,
            participant.JoinedAtUtc,
            participant.RespondedAtUtc);
}
