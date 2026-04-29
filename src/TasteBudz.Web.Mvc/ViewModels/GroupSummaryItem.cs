using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class GroupSummaryItem
{
    public Guid GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Visibility { get; init; } = string.Empty;
    public GroupWallpaperTheme WallpaperTheme { get; init; }
    public int ActiveMembers { get; init; }
    public bool IsPublic { get; init; }
    public bool IsCurrentUserMember { get; init; }
    public string Monogram => GroupCardFormatting.GetInitial(Name);
    public string MemberLabel => $"{ActiveMembers} {(ActiveMembers == 1 ? "member" : "members")}";
    public string DescriptionPreview => string.IsNullOrWhiteSpace(Description)
        ? "Built for neighborhood dinners, shared cravings, and low-friction planning."
        : GroupCardFormatting.Truncate(Description, 120);
    public string BrowseSupportText => IsPublic
        ? "Jump in now and start planning with the current members."
        : "Private access is curated by the owner through invites.";
    public string VisibilitySummary => IsPublic ? "Open group" : "Invite only";
    public string VisibilityLabel => IsPublic ? "Public" : "Private";
    public string? MembershipHint => IsCurrentUserMember ? "You're in" : null;
    public string WallpaperCssClass => $"group-wallpaper--{WallpaperTheme.ToString().ToLowerInvariant()}";

    public static GroupSummaryItem FromDto(GroupSummaryDto dto, bool isCurrentUserMember) => new()
    {
        GroupId = dto.GroupId,
        Name = dto.Name,
        Description = dto.Description,
        Visibility = dto.Visibility.ToString(),
        WallpaperTheme = dto.WallpaperTheme,
        ActiveMembers = dto.ActiveMembers,
        IsPublic = dto.Visibility == GroupVisibility.Public,
        IsCurrentUserMember = isCurrentUserMember,
    };

    public static GroupSummaryItem FromDashboardDto(DashboardGroupSummaryDto dto) => new()
    {
        GroupId = dto.GroupId,
        Name = dto.Name,
        Description = dto.Description,
        Visibility = dto.Visibility.ToString(),
        WallpaperTheme = dto.WallpaperTheme,
        ActiveMembers = dto.ActiveMemberCount,
        IsPublic = dto.Visibility == GroupVisibility.Public,
        IsCurrentUserMember = true,
    };
}
