// User-scoped event read models used by dashboard-style endpoints.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Builds user-scoped event summaries while applying lifecycle synchronization.
/// </summary>
public sealed class UserEventQueryService(
    IEventRepository eventRepository,
    EventLifecycleService lifecycleService)
{
    public async Task<IReadOnlyCollection<UserEventSummary>> ListActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var participations = await eventRepository.ListParticipantsForUserAsync(userId, cancellationToken);
        var eventIds = participations
            .Where(participant => participant.State is EventParticipantState.Joined or EventParticipantState.Invited)
            .Select(participant => participant.EventId)
            .ToHashSet();

        if (eventIds.Count == 0)
        {
            return Array.Empty<UserEventSummary>();
        }

        var events = await eventRepository.ListAsync(cancellationToken);
        var items = new List<UserEventSummary>(eventIds.Count);

        foreach (var eventRecord in events.Where(candidate => eventIds.Contains(candidate.Id)))
        {
            var synchronized = await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);

            if (synchronized.Status is EventStatus.Cancelled or EventStatus.Completed)
            {
                continue;
            }

            items.Add(new UserEventSummary(
                synchronized.Id,
                synchronized.Title,
                synchronized.Status,
                synchronized.EventStartAtUtc));
        }

        return items
            .OrderBy(item => item.EventStartAtUtc)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<UserEventInviteSummary>> ListPendingInvitesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var participations = await eventRepository.ListParticipantsForUserAsync(userId, cancellationToken);
        var inviteMap = participations
            .Where(participant => participant.State == EventParticipantState.Invited)
            .ToDictionary(participant => participant.EventId);

        if (inviteMap.Count == 0)
        {
            return Array.Empty<UserEventInviteSummary>();
        }

        var events = await eventRepository.ListAsync(cancellationToken);
        var items = new List<UserEventInviteSummary>(inviteMap.Count);

        foreach (var eventRecord in events.Where(candidate => inviteMap.ContainsKey(candidate.Id) && candidate.EventType == EventType.Closed))
        {
            var synchronized = await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);

            // Pending invites are actionable only before DecisionAt while the event remains open/full.
            if (synchronized.Status is not EventStatus.Open and not EventStatus.Full)
            {
                continue;
            }

            items.Add(new UserEventInviteSummary(
                synchronized.Id,
                synchronized.Title,
                synchronized.EventType,
                synchronized.EventStartAtUtc,
                inviteMap[synchronized.Id].InvitedAtUtc));
        }

        return items
            .OrderBy(item => item.EventStartAtUtc)
            .ToArray();
    }
}

public sealed record UserEventSummary(
    Guid EventId,
    string? Title,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc);

public sealed record UserEventInviteSummary(
    Guid EventId,
    string? Title,
    EventType EventType,
    DateTimeOffset EventStartAtUtc,
    DateTimeOffset? InvitedAtUtc);
