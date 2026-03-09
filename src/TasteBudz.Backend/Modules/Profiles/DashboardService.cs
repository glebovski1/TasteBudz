// Composes the current user's dashboard-style read models from multiple modules.
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
    UserEventQueryService userEventQueryService,
    UserGroupQueryService userGroupQueryService,
    DiscoveryService discoveryService)
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
        var summaries = await userEventQueryService.ListActiveForUserAsync(userId, cancellationToken);
        return summaries
            .Select(summary => new DashboardEventSummaryDto(summary.EventId, summary.Title, summary.Status, summary.EventStartAtUtc))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DashboardGroupSummaryDto>> ListMyGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var groups = await userGroupQueryService.ListActiveForUserAsync(userId, cancellationToken);
        return groups
            .Select(group => new DashboardGroupSummaryDto(group.GroupId, group.Name, group.Visibility))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<EventInviteDto>> ListMyEventInvitesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var invites = await userEventQueryService.ListPendingInvitesForUserAsync(userId, cancellationToken);
        return invites
            .Select(invite => new EventInviteDto(invite.EventId, invite.Title, invite.EventType, invite.EventStartAtUtc, invite.InvitedAtUtc))
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
        var budz = await discoveryService.ListMyBudzAsync(userId, cancellationToken);
        return budz
            .Select(bud => new DashboardBudSummaryDto(bud.UserId, bud.Username, bud.DisplayName))
            .ToArray();
    }
}
