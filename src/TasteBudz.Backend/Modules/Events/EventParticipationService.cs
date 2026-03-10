// Join, leave, decline, and host-removal workflows for event participants.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Owns participant state transitions for open and closed events.
/// </summary>
public sealed class EventParticipationService(
    IEventRepository eventRepository,
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    INotificationService notificationService,
    RestrictionService restrictionService,
    EventLifecycleService lifecycleService,
    IKeyedLockProvider keyedLockProvider,
    IClock clock)
{
    public async Task<EventParticipantDto> JoinOpenEventAsync(CurrentUser currentUser, Guid eventId, CancellationToken cancellationToken = default)
    {
        await restrictionService.EnsureNotRestrictedAsync(
            currentUser.UserId,
            RestrictionScope.EventJoin,
            "You are currently restricted from joining events.",
            cancellationToken);

        await using var eventLock = await keyedLockProvider.AcquireAsync(EventPolicy.GetLockKey(eventId), cancellationToken);

        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);

        if (eventRecord.EventType != EventType.Open)
        {
            throw ApiException.Conflict("Only open events can be joined directly.");
        }

        if (eventRecord.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw ApiException.Conflict("This event can no longer be joined.");
        }

        if (clock.UtcNow >= eventRecord.DecisionAtUtc && !EventPolicy.IsPrivileged(currentUser))
        {
            throw ApiException.Conflict("Participation is locked after DecisionAt.");
        }

        var participants = await eventRepository.ListParticipantsAsync(eventId, cancellationToken);
        var existing = participants.FirstOrDefault(participant => participant.UserId == currentUser.UserId);

        if (existing?.State == EventParticipantState.Joined)
        {
            throw ApiException.Conflict("You are already participating in this event.");
        }

        if (existing?.State == EventParticipantState.Removed)
        {
            throw ApiException.Forbidden("You have been removed from this event.");
        }

        if (participants.Count(participant => participant.State == EventParticipantState.Joined) >= eventRecord.Capacity)
        {
            throw ApiException.Conflict("This event is already full.");
        }

        var now = clock.UtcNow;
        var participant = new EventParticipant(
            eventId,
            currentUser.UserId,
            EventParticipantState.Joined,
            existing?.InvitedAtUtc,
            now,
            existing?.InvitedAtUtc is null ? existing?.RespondedAtUtc : now,
            null,
            null);

        await eventRepository.SaveParticipantAsync(participant, cancellationToken);
        await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
        await NotifyHostAsync(eventRecord, currentUser.UserId, NotificationType.EventJoined, $"{currentUser.Username} joined {eventRecord.Title ?? "your event"}.", cancellationToken);

        return await MapParticipantAsync(participant, cancellationToken);
    }

    public async Task<EventParticipantDto> UpdateMyParticipationAsync(CurrentUser currentUser, Guid eventId, UpdateMyParticipationRequest request, CancellationToken cancellationToken = default)
    {
        var desiredState = request.State ?? throw ApiException.BadRequest("state is required.");

        if (desiredState is not EventParticipantState.Joined and not EventParticipantState.Left and not EventParticipantState.Declined)
        {
            throw ApiException.BadRequest("Only Joined, Left, or Declined can be requested through this endpoint.");
        }

        await using var eventLock = await keyedLockProvider.AcquireAsync(EventPolicy.GetLockKey(eventId), cancellationToken);

        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);

        if (eventRecord.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw ApiException.Conflict("This event can no longer be changed.");
        }

        var participant = await eventRepository.GetParticipantAsync(eventId, currentUser.UserId, cancellationToken)
            ?? throw ApiException.Conflict("You do not have a participation record for this event.");
        var canBypassDecisionLock = EventPolicy.IsPrivileged(currentUser);

        if (clock.UtcNow >= eventRecord.DecisionAtUtc && !canBypassDecisionLock)
        {
            throw ApiException.Conflict("Participation is locked after DecisionAt.");
        }

        EventParticipant updated;
        var now = clock.UtcNow;

        switch (desiredState)
        {
            case EventParticipantState.Joined:
                await restrictionService.EnsureNotRestrictedAsync(
                    currentUser.UserId,
                    RestrictionScope.EventJoin,
                    "You are currently restricted from joining events.",
                    cancellationToken);

                if (eventRecord.EventType != EventType.Closed)
                {
                    throw ApiException.BadRequest("Use the join endpoint for open events.");
                }

                if (participant.State == EventParticipantState.Removed)
                {
                    throw ApiException.Forbidden("You have been removed from this event.");
                }

                if (participant.State == EventParticipantState.Joined)
                {
                    return await MapParticipantAsync(participant, cancellationToken);
                }

                var participants = await eventRepository.ListParticipantsAsync(eventId, cancellationToken);

                // Closed-event invite acceptance still checks capacity at acceptance time.
                if (participants.Count(existing => existing.State == EventParticipantState.Joined) >= eventRecord.Capacity)
                {
                    throw ApiException.Conflict("This event is already full.");
                }

                updated = participant with
                {
                    State = EventParticipantState.Joined,
                    JoinedAtUtc = now,
                    RespondedAtUtc = now,
                    LeftAtUtc = null,
                    RemovedAtUtc = null,
                };

                await eventRepository.SaveParticipantAsync(updated, cancellationToken);
                await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
                await NotifyHostAsync(eventRecord, currentUser.UserId, NotificationType.EventJoined, $"{currentUser.Username} joined {eventRecord.Title ?? "your event"}.", cancellationToken);
                return await MapParticipantAsync(updated, cancellationToken);

            case EventParticipantState.Left:
                if (participant.UserId == eventRecord.HostUserId)
                {
                    throw ApiException.BadRequest("The event host cannot leave their own event.");
                }

                if (participant.State != EventParticipantState.Joined)
                {
                    throw ApiException.Conflict("You can only leave an event after joining it.");
                }

                updated = participant with
                {
                    State = EventParticipantState.Left,
                    RespondedAtUtc = now,
                    LeftAtUtc = now,
                };

                await eventRepository.SaveParticipantAsync(updated, cancellationToken);
                await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
                await NotifyHostAsync(eventRecord, currentUser.UserId, NotificationType.EventLeft, $"{currentUser.Username} left {eventRecord.Title ?? "your event"}.", cancellationToken);
                return await MapParticipantAsync(updated, cancellationToken);

            case EventParticipantState.Declined:
                if (eventRecord.EventType != EventType.Closed)
                {
                    throw ApiException.BadRequest("Only closed-event invites can be declined.");
                }

                if (participant.UserId == eventRecord.HostUserId)
                {
                    throw ApiException.BadRequest("The event host cannot decline their own event.");
                }

                if (participant.State == EventParticipantState.Joined)
                {
                    throw ApiException.Conflict("Use LEFT to leave an event you already joined.");
                }

                if (participant.State == EventParticipantState.Removed)
                {
                    throw ApiException.Forbidden("You have been removed from this event.");
                }

                updated = participant with
                {
                    State = EventParticipantState.Declined,
                    RespondedAtUtc = now,
                    LeftAtUtc = null,
                    RemovedAtUtc = null,
                };

                await eventRepository.SaveParticipantAsync(updated, cancellationToken);
                await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
                return await MapParticipantAsync(updated, cancellationToken);

            default:
                throw ApiException.BadRequest("Unsupported participation state.");
        }
    }

    public async Task RemoveParticipantAsync(CurrentUser currentUser, Guid eventId, Guid participantUserId, CancellationToken cancellationToken = default)
    {
        await using var eventLock = await keyedLockProvider.AcquireAsync(EventPolicy.GetLockKey(eventId), cancellationToken);

        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);
        var isPrivileged = EventPolicy.IsPrivileged(currentUser);

        if (currentUser.UserId != eventRecord.HostUserId && !isPrivileged)
        {
            throw ApiException.Forbidden("Only the event host or a moderator can remove participants.");
        }

        if (currentUser.UserId == eventRecord.HostUserId && clock.UtcNow >= eventRecord.DecisionAtUtc && !isPrivileged)
        {
            throw ApiException.Conflict("Participants cannot be removed after DecisionAt.");
        }

        if (participantUserId == eventRecord.HostUserId)
        {
            throw ApiException.BadRequest("The event host cannot be removed.");
        }

        var participant = await eventRepository.GetParticipantAsync(eventId, participantUserId, cancellationToken)
            ?? throw ApiException.NotFound("The requested participant could not be found.");

        if (participant.State == EventParticipantState.Removed)
        {
            return;
        }

        var now = clock.UtcNow;
        var updated = participant with
        {
            State = EventParticipantState.Removed,
            RespondedAtUtc = now,
            RemovedAtUtc = now,
        };

        await eventRepository.SaveParticipantAsync(updated, cancellationToken);
        await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
    }

    private async Task<Event> GetSynchronizedEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var eventRecord = await eventRepository.GetAsync(eventId, cancellationToken)
            ?? throw ApiException.NotFound("The requested event could not be found.");

        return await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
    }

    private async Task<EventParticipantDto> MapParticipantAsync(EventParticipant participant, CancellationToken cancellationToken)
    {
        var account = await authRepository.GetByIdAsync(participant.UserId, cancellationToken)
            ?? throw ApiException.NotFound("The requested participant could not be found.");
        var profile = await profileRepository.GetProfileAsync(participant.UserId, cancellationToken);
        return EventDtoMapper.ToParticipant(participant, account, profile);
    }

    private async Task NotifyHostAsync(
        Event eventRecord,
        Guid actorUserId,
        NotificationType notificationType,
        string message,
        CancellationToken cancellationToken)
    {
        if (actorUserId == eventRecord.HostUserId)
        {
            return;
        }

        await notificationService.CreateAsync(
            new Notification(Guid.NewGuid(), eventRecord.HostUserId, notificationType, "Event", eventRecord.Id, message, clock.UtcNow, null),
            cancellationToken);
    }
}
