using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class GroupManageViewModel
{
    public Guid GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Visibility { get; init; } = string.Empty;
    public GroupWallpaperTheme WallpaperTheme { get; init; }
    public bool IsCurrentUserOwner { get; init; }
    public bool IsCurrentUserMember { get; init; }
    public IReadOnlyList<GroupMemberItem> Members { get; init; } = [];
    public IReadOnlyList<GroupEventHistoryItem> EventHistory { get; init; } = [];
    public IReadOnlyList<GroupAnnouncementItem> Announcements { get; init; } = [];
    public IReadOnlyList<GroupEventHistoryItem> PlannedEvents => EventHistory
        .Where(groupEvent => groupEvent.IsPlanned)
        .ToList();
    public IReadOnlyList<GroupEventHistoryItem> HistoryEvents => EventHistory
        .Where(groupEvent => groupEvent.IsHistory)
        .ToList();

    // Edit sub-form pre-populated for the owner settings panel.
    public string? EditName { get; set; }
    public string? EditDescription { get; set; }
    public GroupVisibility? EditVisibility { get; set; }
    public GroupWallpaperTheme? EditWallpaperTheme { get; set; }

    // Invite and announcement sub-form fields are posted from the manage page.
    public string? InviteUsername { get; set; }
    public string? AnnouncementTitle { get; set; }
    public string? AnnouncementBody { get; set; }
    public int ActiveMemberCount => Members.Count;
    public int LinkedEventCount => EventHistory.Count;
    public int AnnouncementCount => Announcements.Count;
    public GroupMemberItem? Owner => Members.FirstOrDefault(member => member.IsOwner);
    public string WallpaperCssClass => $"group-wallpaper--{WallpaperTheme.ToString().ToLowerInvariant()}";
    public IReadOnlyList<GroupWallpaperOption> WallpaperOptions => GroupWallpaperOptions.All;
    public string VisibilitySummary => string.Equals(Visibility, "Private", StringComparison.OrdinalIgnoreCase)
        ? "Private circle"
        : "Open community";
    public string JoiningSummary => string.Equals(Visibility, "Private", StringComparison.OrdinalIgnoreCase)
        ? "New members join by invitation from the owner."
        : "Anyone on TasteBudz can join while the group is active.";

    public static GroupManageViewModel FromDto(
        GroupDetailDto dto,
        Guid currentUserId,
        IReadOnlyList<GroupEventHistoryItem>? eventHistory = null,
        IReadOnlyList<GroupAnnouncementItem>? announcements = null) => new()
    {
        GroupId = dto.GroupId,
        Name = dto.Name,
        Description = dto.Description,
        Visibility = dto.Visibility.ToString(),
        WallpaperTheme = dto.WallpaperTheme,
        IsCurrentUserOwner = dto.OwnerUserId == currentUserId,
        IsCurrentUserMember = dto.IsCurrentUserMember,
        Members = dto.Members
            .Where(m => m.State == GroupMemberState.Active)
            .Select(m => GroupMemberItem.FromDto(m, dto.OwnerUserId))
            .ToList(),
        EventHistory = eventHistory ?? [],
        Announcements = announcements ?? [],
        EditName = dto.Name,
        EditDescription = dto.Description,
        EditVisibility = dto.Visibility,
        EditWallpaperTheme = dto.WallpaperTheme,
    };
}
