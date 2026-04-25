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
        var items = await ListForUserAsync(userId, Array.Empty<Guid>(), cancellationToken);
        return items
            .Where(item => item.Status is not EventStatus.Cancelled and not EventStatus.Completed)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<UserEventSummary>> ListForUserAsync(
        Guid userId,
        IReadOnlyCollection<Guid> activeGroupIds,
        CancellationToken cancellationToken = default)
    {
        var participations = await eventRepository.ListParticipantsForUserAsync(userId, cancellationToken);
        var participationByEventId = participations
            .Where(participant => participant.State is EventParticipantState.Joined or EventParticipantState.Invited)
            .GroupBy(participant => participant.EventId)
            .ToDictionary(group => group.Key, group => group.First());
        var groupIds = activeGroupIds.ToHashSet();

        var events = await eventRepository.ListAsync(cancellationToken);
        var items = new List<UserEventSummary>();

        foreach (var eventRecord in events)
        {
            participationByEventId.TryGetValue(eventRecord.Id, out var participation);
            var isHosted = eventRecord.HostUserId == userId;
            var isJoined = participation?.State == EventParticipantState.Joined;
            var isInvited = participation?.State == EventParticipantState.Invited;
            var isGroupLinked = eventRecord.GroupId.HasValue && groupIds.Contains(eventRecord.GroupId.Value);

            if (!isHosted && !isJoined && !isInvited && !isGroupLinked)
            {
                continue;
            }

            var synchronized = await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);

            items.Add(new UserEventSummary(
                synchronized.Id,
                synchronized.Title,
                synchronized.EventType,
                synchronized.Status,
                synchronized.EventStartAtUtc,
                synchronized.CuisineTarget,
                synchronized.GroupId,
                isHosted,
                isJoined,
                isInvited,
                isGroupLinked));
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
    EventType EventType,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc,
    string? CuisineTarget,
    Guid? GroupId = null,
    bool IsHosted = false,
    bool IsJoined = false,
    bool IsInvited = false,
    bool IsGroupLinked = false);

public sealed record UserEventInviteSummary(
    Guid EventId,
    string? Title,
    EventType EventType,
    DateTimeOffset EventStartAtUtc,
    DateTimeOffset? InvitedAtUtc);
