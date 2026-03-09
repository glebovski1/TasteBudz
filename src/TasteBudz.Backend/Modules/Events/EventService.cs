// Event creation, retrieval, update, cancellation, and participant-list workflows.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Owns the core event aggregate workflows while keeping HTTP concerns in controllers.
/// </summary>
public sealed class EventService(
    IEventRepository eventRepository,
    IRestaurantRepository restaurantRepository,
    IGroupRepository groupRepository,
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    INotificationService notificationService,
    EventLifecycleService lifecycleService,
    EventInviteService eventInviteService,
    IKeyedLockProvider keyedLockProvider,
    IClock clock)
{
    /// <summary>
    /// Creates a new event, auto-joins the host, and optionally seeds closed-event invite records.
    /// </summary>
    public async Task<EventDetailDto> CreateAsync(CurrentUser currentUser, CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        // Required inputs are extracted once up front so the rest of the workflow can rely on concrete values.
        var eventType = request.EventType ?? throw ApiException.BadRequest("eventType is required.");
        var eventStartAtUtc = request.EventStartAtUtc ?? throw ApiException.BadRequest("eventStartAtUtc is required.");
        var capacity = request.Capacity ?? throw ApiException.BadRequest("capacity is required.");
        var title = NormalizeOptional(request.Title);
        var cuisineTarget = NormalizeOptional(request.CuisineTarget);

        // Capacity is a business invariant, not just an HTTP annotation, so enforce it in the service layer too.
        EnsureCapacityInRange(capacity);
        EventPolicy.EnsureValidLocationSelection(request.SelectedRestaurantId, cuisineTarget);

        if (eventType == EventType.Open && request.InviteUsernames.Count > 0)
        {
            throw ApiException.BadRequest("Open events do not accept inviteUsernames during creation.");
        }

        await EnsureRestaurantExistsAsync(request.SelectedRestaurantId, cancellationToken);
        await EnsureCanLinkGroupAsync(currentUser, request.GroupId, cancellationToken);

        // Closed-event invite usernames are resolved up front so create either succeeds fully or fails early.
        var invitees = eventType == EventType.Closed && request.InviteUsernames.Count > 0
            ? await eventInviteService.ResolveInviteesAsync(currentUser, request.InviteUsernames, cancellationToken)
            : Array.Empty<UserAccount>();
        // `now` is captured once so the event row and host participant share the same creation timestamp.
        var now = clock.UtcNow;
        var eventRecord = new Event(
            Guid.NewGuid(),
            currentUser.UserId,
            title,
            eventType,
            EventStatus.Open,
            eventStartAtUtc,
            EventPolicy.CalculateDecisionAt(eventType, eventStartAtUtc),
            capacity,
            2,
            request.SelectedRestaurantId,
            cuisineTarget,
            request.GroupId,
            null,
            now,
            now,
            null,
            null);

        await eventRepository.SaveAsync(eventRecord, cancellationToken);
        // Hosts count as the first joined participant immediately after event creation.
        await eventRepository.SaveParticipantAsync(
            new EventParticipant(eventRecord.Id, currentUser.UserId, EventParticipantState.Joined, null, now, now, null, null),
            cancellationToken);

        if (invitees.Length > 0)
        {
            await eventInviteService.InviteResolvedAsync(currentUser, eventRecord, invitees, cancellationToken);
        }

        var synchronized = await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
        return await MapDetailAsync(synchronized, cancellationToken);
    }

    /// <summary>
    /// Returns one event detail view after applying any time-driven lifecycle transitions.
    /// </summary>
    public async Task<EventDetailDto> GetAsync(Guid currentUserId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);
        await EnsureCanViewAsync(currentUserId, eventRecord, cancellationToken);
        return await MapDetailAsync(eventRecord, cancellationToken);
    }

    /// <summary>
    /// Applies host-managed edits while preserving server-owned lifecycle and capacity rules.
    /// </summary>
    public async Task<EventDetailDto> UpdateAsync(Guid currentUserId, Guid eventId, UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        // Per-event locking keeps concurrent updates from producing inconsistent capacity or invite state.
        await using var eventLock = await keyedLockProvider.AcquireAsync(EventPolicy.GetLockKey(eventId), cancellationToken);

        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);

        if (eventRecord.HostUserId != currentUserId)
        {
            throw ApiException.Forbidden("Only the event host can update this event.");
        }

        if (eventRecord.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw ApiException.Conflict("Only active events can be updated.");
        }

        var participants = await eventRepository.ListParticipantsAsync(eventId, cancellationToken);
        // Joined participants are the only rows that consume event capacity.
        var activeParticipants = participants.Count(participant => participant.State == EventParticipantState.Joined);
        var selectedRestaurantId = eventRecord.SelectedRestaurantId;
        var cuisineTarget = eventRecord.CuisineTarget;

        if (request.SelectedRestaurantId.HasValue && !string.IsNullOrWhiteSpace(request.CuisineTarget))
        {
            throw ApiException.BadRequest("Provide either selectedRestaurantId or cuisineTarget, not both.");
        }

        if (request.SelectedRestaurantId.HasValue)
        {
            await EnsureRestaurantExistsAsync(request.SelectedRestaurantId, cancellationToken);
            selectedRestaurantId = request.SelectedRestaurantId;
            cuisineTarget = null;
        }
        else if (request.CuisineTarget is not null)
        {
            cuisineTarget = NormalizeOptional(request.CuisineTarget);
            selectedRestaurantId = null;
        }

        EventPolicy.EnsureValidLocationSelection(selectedRestaurantId, cuisineTarget);

        var groupId = request.GroupId ?? eventRecord.GroupId;
        await EnsureCanLinkGroupAsync(currentUserId, groupId, cancellationToken);

        var capacity = request.Capacity ?? eventRecord.Capacity;

        // Re-check the invariant here so non-HTTP callers cannot bypass the documented size bounds.
        EnsureCapacityInRange(capacity);

        if (activeParticipants > capacity)
        {
            throw ApiException.Conflict("Capacity cannot be reduced below the current joined participant count.");
        }

        var updatedStartAtUtc = request.EventStartAtUtc ?? eventRecord.EventStartAtUtc;
        var updatedTitle = request.Title is null ? eventRecord.Title : NormalizeOptional(request.Title);
        // This flag avoids useless writes and notifications when the patch did not materially change the event.
        var hasMaterialChanges = eventRecord.Title != updatedTitle ||
                                 eventRecord.EventStartAtUtc != updatedStartAtUtc ||
                                 eventRecord.Capacity != capacity ||
                                 eventRecord.SelectedRestaurantId != selectedRestaurantId ||
                                 !string.Equals(eventRecord.CuisineTarget, cuisineTarget, StringComparison.Ordinal) ||
                                 eventRecord.GroupId != groupId;

        if (!hasMaterialChanges)
        {
            return await MapDetailAsync(eventRecord, cancellationToken);
        }

        // DecisionAt is recalculated whenever the scheduled start time changes.
        var candidate = eventRecord with
        {
            Title = updatedTitle,
            EventStartAtUtc = updatedStartAtUtc,
            DecisionAtUtc = EventPolicy.CalculateDecisionAt(eventRecord.EventType, updatedStartAtUtc),
            Capacity = capacity,
            SelectedRestaurantId = selectedRestaurantId,
            CuisineTarget = cuisineTarget,
            GroupId = groupId,
            UpdatedAtUtc = clock.UtcNow,
        };

        await eventRepository.SaveAsync(candidate, cancellationToken);
        var synchronized = await lifecycleService.SynchronizeAsync(candidate, cancellationToken);
        await NotifyParticipantsOfUpdateAsync(candidate, participants, cancellationToken);

        return await MapDetailAsync(synchronized, cancellationToken);
    }

    /// <summary>
    /// Lists event participants after the caller's visibility rights have been confirmed.
    /// </summary>
    public async Task<IReadOnlyCollection<EventParticipantDto>> ListParticipantsAsync(Guid currentUserId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);
        await EnsureCanViewAsync(currentUserId, eventRecord, cancellationToken);

        var participants = await eventRepository.ListParticipantsAsync(eventId, cancellationToken);
        var items = new List<EventParticipantDto>(participants.Count);

        foreach (var participant in participants)
        {
            var account = await authRepository.GetByIdAsync(participant.UserId, cancellationToken)
                ?? throw ApiException.NotFound("The requested participant could not be found.");
            var profile = await profileRepository.GetProfileAsync(participant.UserId, cancellationToken);
            items.Add(EventDtoMapper.ToParticipant(participant, account, profile));
        }

        return items;
    }

    /// <summary>
    /// Cancels an active event on behalf of its host and notifies affected participants.
    /// </summary>
    public async Task CancelAsync(Guid currentUserId, Guid eventId, CancelEventRequest request, CancellationToken cancellationToken = default)
    {
        await using var eventLock = await keyedLockProvider.AcquireAsync(EventPolicy.GetLockKey(eventId), cancellationToken);

        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);

        if (eventRecord.HostUserId != currentUserId)
        {
            throw ApiException.Forbidden("Only the event host can cancel this event.");
        }

        if (eventRecord.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw ApiException.Conflict("Only active events can be cancelled.");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? throw ApiException.BadRequest("reason is required.")
            : request.Reason.Trim();
        var now = clock.UtcNow;
        var cancelled = eventRecord with
        {
            Status = EventStatus.Cancelled,
            CancellationReason = reason,
            CancelledAtUtc = now,
            UpdatedAtUtc = now,
        };

        await eventRepository.SaveAsync(cancelled, cancellationToken);
        await NotifyCancellationAsync(cancelled, cancellationToken);
    }

    /// <summary>
    /// Loads one event and applies time-based lifecycle recalculation before any caller uses it.
    /// </summary>
    private async Task<Event> GetSynchronizedEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var eventRecord = await eventRepository.GetAsync(eventId, cancellationToken)
            ?? throw ApiException.NotFound("The requested event could not be found.");

        return await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
    }

    /// <summary>
    /// Enforces the rule that closed events are only visible to the host or to still-relevant participants.
    /// </summary>
    private async Task EnsureCanViewAsync(Guid currentUserId, Event eventRecord, CancellationToken cancellationToken)
    {
        if (eventRecord.EventType == EventType.Open || eventRecord.HostUserId == currentUserId)
        {
            return;
        }

        // Closed events are hidden unless the caller is the host or still has a visible participation record.
        var participant = await eventRepository.GetParticipantAsync(eventRecord.Id, currentUserId, cancellationToken);

        if (participant is null || participant.State == EventParticipantState.Removed)
        {
            throw ApiException.NotFound("The requested event could not be found.");
        }
    }

    /// <summary>
    /// Builds the detail DTO and recalculates the active participant count from current participant rows.
    /// </summary>
    private async Task<EventDetailDto> MapDetailAsync(Event eventRecord, CancellationToken cancellationToken)
    {
        var participants = await eventRepository.ListParticipantsAsync(eventRecord.Id, cancellationToken);
        return EventDtoMapper.ToDetail(eventRecord, participants.Count(participant => participant.State == EventParticipantState.Joined));
    }

    /// <summary>
    /// Sends the material-update notification only to non-host users who still have a meaningful relationship to the event.
    /// </summary>
    private async Task NotifyParticipantsOfUpdateAsync(
        Event eventRecord,
        IReadOnlyCollection<EventParticipant> participants,
        CancellationToken cancellationToken)
    {
        // Joined and invited participants are the people who still need to react to the changed event details.
        var recipientIds = participants
            .Where(participant => participant.State is EventParticipantState.Joined or EventParticipantState.Invited)
            .Select(participant => participant.UserId)
            .Where(userId => userId != eventRecord.HostUserId)
            .Distinct()
            .ToArray();
        var message = $"{eventRecord.Title ?? "An event"} was updated.";
        var now = clock.UtcNow;

        foreach (var recipientId in recipientIds)
        {
            await notificationService.CreateAsync(
                new Notification(Guid.NewGuid(), recipientId, NotificationType.EventUpdated, "Event", eventRecord.Id, message, now, null),
                cancellationToken);
        }
    }

    /// <summary>
    /// Sends cancellation notifications to the non-host users still attached to the event.
    /// </summary>
    private async Task NotifyCancellationAsync(Event eventRecord, CancellationToken cancellationToken)
    {
        var participants = await eventRepository.ListParticipantsAsync(eventRecord.Id, cancellationToken);
        var recipientIds = participants
            .Where(participant => participant.State is EventParticipantState.Joined or EventParticipantState.Invited)
            .Select(participant => participant.UserId)
            .Where(userId => userId != eventRecord.HostUserId)
            .Distinct()
            .ToArray();
        var message = eventRecord.CancellationReason ?? "Your event was cancelled.";
        var now = clock.UtcNow;

        foreach (var recipientId in recipientIds)
        {
            await notificationService.CreateAsync(
                new Notification(Guid.NewGuid(), recipientId, NotificationType.EventCancelled, "Event", eventRecord.Id, message, now, null),
                cancellationToken);
        }
    }

    /// <summary>
    /// Confirms that a referenced restaurant exists before an event stores its identifier.
    /// </summary>
    private async Task EnsureRestaurantExistsAsync(Guid? restaurantId, CancellationToken cancellationToken)
    {
        if (!restaurantId.HasValue)
        {
            return;
        }

        _ = await restaurantRepository.GetAsync(restaurantId.Value, cancellationToken)
            ?? throw ApiException.NotFound("The selected restaurant could not be found.");
    }

    /// <summary>
    /// Confirms that a referenced group exists and is still active before linking it to an event.
    /// </summary>
    private async Task EnsureCanLinkGroupAsync(CurrentUser currentUser, Guid? groupId, CancellationToken cancellationToken) =>
        await EnsureCanLinkGroupAsync(currentUser.UserId, groupId, cancellationToken);

    private async Task EnsureCanLinkGroupAsync(Guid currentUserId, Guid? groupId, CancellationToken cancellationToken)
    {
        if (!groupId.HasValue)
        {
            return;
        }

        var group = await groupRepository.GetAsync(groupId.Value, cancellationToken)
            ?? throw ApiException.NotFound("The selected group could not be found.");

        if (group.LifecycleState != GroupLifecycleState.Active)
        {
            throw ApiException.Conflict("Only active groups can be linked to events.");
        }

        if (group.OwnerUserId == currentUserId)
        {
            return;
        }

        var membership = await groupRepository.GetMemberAsync(group.Id, currentUserId, cancellationToken);

        if (membership?.State != GroupMemberState.Active)
        {
            throw ApiException.Forbidden("Only active group members can link an event to that group.");
        }
    }

    /// <summary>
    /// Trims optional text fields and converts blank strings to null so storage stays consistent.
    /// </summary>
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    /// <summary>
    /// Enforces the documented hard event-size limits inside the service layer itself.
    /// </summary>
    private static void EnsureCapacityInRange(int capacity)
    {
        if (capacity is < 2 or > 8)
        {
            throw ApiException.BadRequest("capacity must be between 2 and 8.");
        }
    }
}
