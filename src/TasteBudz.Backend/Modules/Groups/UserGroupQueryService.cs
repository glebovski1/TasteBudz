// User-scoped group read models used by dashboard-style endpoints.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

/// <summary>
/// Builds active-group summaries for one user.
/// </summary>
public sealed class UserGroupQueryService(IGroupRepository groupRepository)
{
    public async Task<IReadOnlyCollection<UserGroupSummary>> ListActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var memberships = await groupRepository.ListMembershipsForUserAsync(userId, cancellationToken);
        var activeGroupIds = memberships
            .Where(member => member.State == GroupMemberState.Active)
            .Select(member => member.GroupId)
            .ToHashSet();

        if (activeGroupIds.Count == 0)
        {
            return Array.Empty<UserGroupSummary>();
        }

        var groups = await groupRepository.ListAsync(cancellationToken);
        return groups
            .Where(group => activeGroupIds.Contains(group.Id) && group.LifecycleState == GroupLifecycleState.Active)
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new UserGroupSummary(group.Id, group.Name, group.Visibility))
            .ToArray();
    }
}

public sealed record UserGroupSummary(
    Guid GroupId,
    string Name,
    GroupVisibility Visibility);
