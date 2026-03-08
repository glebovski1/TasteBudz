// Composes the current user's dashboard-style read models from multiple modules.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Builds lightweight dashboard projections without exposing persistence models directly.
/// </summary>
public sealed class DashboardService(
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    IEventRepository eventRepository,
    IGroupRepository groupRepository,
    IDiscoveryRepository discoveryRepository)
{
    public async Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(userId, cancellationToken);
        var events = await ListMyEventsAsync(userId, cancellationToken);
        var groups = await ListMyGroupsAsync(userId, cancellationToken);
        var budz = await ListMyBudzAsync(userId, cancellationToken);

        return new DashboardDto(profile, events, groups, budz);
    }

    public async Task<IReadOnlyCollection<DashboardEventSummaryDto>> ListMyEventsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var participations = await eventRepository.ListParticipantsForUserAsync(userId, cancellationToken);
        var activeStates = new[] { EventParticipantState.Joined, EventParticipantState.Invited };
        var eventIds = participations.Where(participant => activeStates.Contains(participant.State)).Select(participant => participant.EventId).ToHashSet();

        var events = await eventRepository.ListAsync(cancellationToken);
        return events
            .Where(eventRecord => eventIds.Contains(eventRecord.Id) && eventRecord.Status is not EventStatus.Cancelled and not EventStatus.Completed)
            .OrderBy(eventRecord => eventRecord.EventStartAtUtc)
            .Select(eventRecord => new DashboardEventSummaryDto(eventRecord.Id, eventRecord.Title, eventRecord.Status, eventRecord.EventStartAtUtc))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DashboardGroupSummaryDto>> ListMyGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var memberships = await groupRepository.ListMembershipsForUserAsync(userId, cancellationToken);
        var activeGroupIds = memberships.Where(member => member.State == GroupMemberState.Active).Select(member => member.GroupId).ToHashSet();
        var groups = await groupRepository.ListAsync(cancellationToken);

        return groups
            .Where(group => activeGroupIds.Contains(group.Id) && group.LifecycleState == GroupLifecycleState.Active)
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DashboardGroupSummaryDto(group.Id, group.Name, group.Visibility))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<EventInviteDto>> ListMyEventInvitesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var participations = await eventRepository.ListParticipantsForUserAsync(userId, cancellationToken);
        var inviteMap = participations.Where(participant => participant.State == EventParticipantState.Invited)
            .ToDictionary(participant => participant.EventId);
        var events = await eventRepository.ListAsync(cancellationToken);

        return events
            .Where(eventRecord => inviteMap.ContainsKey(eventRecord.Id) && eventRecord.EventType == EventType.Closed)
            .OrderBy(eventRecord => eventRecord.EventStartAtUtc)
            .Select(eventRecord =>
            {
                var participant = inviteMap[eventRecord.Id];
                return new EventInviteDto(eventRecord.Id, eventRecord.Title, eventRecord.EventType, eventRecord.EventStartAtUtc, participant.InvitedAtUtc);
            })
            .ToArray();
    }

    private async Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current account could not be found.");
        var profile = await profileRepository.GetProfileAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current profile could not be found.");

        return new ProfileDto(account.Id, account.Username, account.Email, profile.DisplayName, profile.Bio, profile.HomeAreaZipCode, profile.SocialGoal);
    }

    private async Task<IReadOnlyCollection<DashboardBudSummaryDto>> ListMyBudzAsync(Guid userId, CancellationToken cancellationToken)
    {
        var accounts = (await authRepository.ListActiveAccountsAsync(cancellationToken)).ToDictionary(account => account.Id);
        var profiles = (await profileRepository.ListProfilesAsync(cancellationToken)).ToDictionary(profile => profile.UserId);
        var connections = await discoveryRepository.ListBudConnectionsAsync(cancellationToken);

        return connections
            .Where(connection => connection.State == BudConnectionState.Connected && (connection.UserOneId == userId || connection.UserTwoId == userId))
            .Select(connection => connection.UserOneId == userId ? connection.UserTwoId : connection.UserOneId)
            .Where(otherUserId => accounts.ContainsKey(otherUserId))
            .OrderBy(otherUserId => accounts[otherUserId].Username, StringComparer.OrdinalIgnoreCase)
            .Select(otherUserId => new DashboardBudSummaryDto(otherUserId, accounts[otherUserId].Username, profiles.GetValueOrDefault(otherUserId)?.DisplayName ?? accounts[otherUserId].Username))
            .ToArray();
    }
}