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
        var activeGroups = groups
            .Where(group => activeGroupIds.Contains(group.Id) && group.LifecycleState == GroupLifecycleState.Active)
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var summaries = new List<UserGroupSummary>(activeGroups.Length);
        foreach (var group in activeGroups)
        {
            var activeMembers = await groupRepository.ListMembersAsync(group.Id, cancellationToken);
            summaries.Add(new UserGroupSummary(
                group.Id,
                group.Name,
                group.Description,
                group.Visibility,
                activeMembers.Count(member => member.State == GroupMemberState.Active))
            {
                WallpaperTheme = group.WallpaperTheme,
            });
        }

        return summaries;
    }
}

public sealed record UserGroupSummary(
    Guid GroupId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    int ActiveMemberCount)
{
    public GroupWallpaperTheme WallpaperTheme { get; init; } = GroupWallpaperTheme.Default;
}
