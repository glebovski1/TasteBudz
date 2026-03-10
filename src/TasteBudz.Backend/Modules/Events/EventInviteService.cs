// Invite workflows for closed events.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Resolves invitees, enforces invite policy, and creates closed-event invite records.
/// </summary>
public sealed class EventInviteService(
    IEventRepository eventRepository,
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    INotificationService notificationService,
    EventLifecycleService lifecycleService,
    IKeyedLockProvider keyedLockProvider,
    IClock clock)
{
    public async Task<IReadOnlyCollection<EventParticipantDto>> InviteAsync(CurrentUser currentUser, Guid eventId, InviteUsersRequest request, CancellationToken cancellationToken = default)
    {
        await using var eventLock = await keyedLockProvider.AcquireAsync(EventPolicy.GetLockKey(eventId), cancellationToken);

        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);
        var invitees = await ResolveInviteesAsync(currentUser, request.Usernames, cancellationToken);
        return await InviteResolvedAsync(currentUser, eventRecord, invitees, cancellationToken);
    }

    internal async Task<UserAccount[]> ResolveInviteesAsync(CurrentUser currentUser, IReadOnlyCollection<string> usernames, CancellationToken cancellationToken = default)
    {
        var normalizedUsernames = usernames
            .Where(username => !string.IsNullOrWhiteSpace(username))
            .Select(username => username.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedUsernames.Length == 0)
        {
            throw ApiException.BadRequest("At least one username is required.");
        }

        var invitees = new List<UserAccount>(normalizedUsernames.Length);

        foreach (var username in normalizedUsernames)
        {
            var invitee = await authRepository.FindByUsernameAsync(username, cancellationToken)
                ?? throw ApiException.NotFound($"User '{username}' could not be found.");

            if (invitee.Id == currentUser.UserId)
            {
                throw ApiException.BadRequest("You cannot invite yourself.");
            }

            await EventPolicy.EnsureNotBlockedAsync(profileRepository, currentUser.UserId, invitee.Id, cancellationToken);
            invitees.Add(invitee);
        }

        return invitees.ToArray();
    }

    internal async Task<IReadOnlyCollection<EventParticipantDto>> InviteResolvedAsync(
        CurrentUser currentUser,
        Event eventRecord,
        IReadOnlyCollection<UserAccount> invitees,
        CancellationToken cancellationToken = default)
    {
        await EnsureHostCanInviteAsync(currentUser, eventRecord, cancellationToken);

        var existingParticipants = (await eventRepository.ListParticipantsAsync(eventRecord.Id, cancellationToken))
            .ToDictionary(participant => participant.UserId);
        var now = clock.UtcNow;
        var invitedParticipants = new List<EventParticipantDto>(invitees.Count);

        foreach (var invitee in invitees)
        {
            if (existingParticipants.TryGetValue(invitee.Id, out var existing) && existing.State == EventParticipantState.Joined)
            {
                throw ApiException.Conflict($"User '{invitee.Username}' is already participating in the event.");
            }
        }

        foreach (var invitee in invitees)
        {
            // Invites do not reserve a seat; capacity is checked again when the user accepts.
            var participant = new EventParticipant(
                eventRecord.Id,
                invitee.Id,
                EventParticipantState.Invited,
                now,
                null,
                null,
                null,
                null);

            await eventRepository.SaveParticipantAsync(participant, cancellationToken);
            existingParticipants[invitee.Id] = participant;

            var profile = await profileRepository.GetProfileAsync(invitee.Id, cancellationToken);
            invitedParticipants.Add(EventDtoMapper.ToParticipant(participant, invitee, profile));

            await notificationService.CreateAsync(
                new Notification(
                    Guid.NewGuid(),
                    invitee.Id,
                    NotificationType.EventInviteReceived,
                    "Event",
                    eventRecord.Id,
                    $"You were invited to {eventRecord.Title ?? "an event"}.",
                    now,
                    null),
                cancellationToken);
        }

        await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
        return invitedParticipants;
    }

    private async Task EnsureHostCanInviteAsync(CurrentUser currentUser, Event eventRecord, CancellationToken cancellationToken)
    {
        if (eventRecord.HostUserId != currentUser.UserId)
        {
            throw ApiException.Forbidden("Only the event host can invite users.");
        }

        if (eventRecord.EventType != EventType.Closed)
        {
            throw ApiException.Conflict("Only closed events accept invites.");
        }

        if (eventRecord.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw ApiException.Conflict("This event can no longer be modified.");
        }

        if (clock.UtcNow >= eventRecord.DecisionAtUtc)
        {
            throw ApiException.Conflict("Invites are locked after DecisionAt.");
        }

        await Task.CompletedTask;
    }

    private async Task<Event> GetSynchronizedEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var eventRecord = await eventRepository.GetAsync(eventId, cancellationToken)
            ?? throw ApiException.NotFound("The requested event could not be found.");

        return await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
    }
}