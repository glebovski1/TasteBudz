using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class GroupIndexViewModel
{
    public IReadOnlyList<GroupSummaryItem> Groups { get; init; } = [];
    public string? SearchQuery { get; init; }
    public int TotalCount { get; init; }
    public int TotalMembers => Groups.Sum(group => group.ActiveMembers);
    public int LargestGroupSize => Groups.Count == 0 ? 0 : Groups.Max(group => group.ActiveMembers);

    public static GroupIndexViewModel Empty => new();

    public static GroupIndexViewModel FromDto(
        IEnumerable<GroupSummaryDto> groups,
        int totalCount,
        string? searchQuery = null,
        IEnumerable<DashboardGroupSummaryDto>? myGroups = null)
    {
        var visibleGroups = new Dictionary<Guid, GroupSummaryItem>();

        foreach (var group in groups.Select(group => GroupSummaryItem.FromDto(group, isCurrentUserMember: false)))
        {
            visibleGroups[group.GroupId] = group;
        }

        foreach (var group in (myGroups ?? Array.Empty<DashboardGroupSummaryDto>())
                     .Where(group => MatchesSearch(group.Name, searchQuery))
                     .Select(GroupSummaryItem.FromDashboardDto))
        {
            visibleGroups[group.GroupId] = group;
        }

        var orderedGroups = visibleGroups.Values
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new()
        {
            Groups = orderedGroups,
            TotalCount = totalCount > 0 ? totalCount : orderedGroups.Count,
            SearchQuery = searchQuery,
        };

        static bool MatchesSearch(string groupName, string? searchQuery) =>
            string.IsNullOrWhiteSpace(searchQuery) ||
            groupName.Contains(searchQuery.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
