using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;

// ── Browse ───────────────────────────────────────────────────────────────────

/// <summary>
/// Page model for the Groups browse/index page.
/// </summary>
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
        int _,
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
            TotalCount = orderedGroups.Count,
            SearchQuery = searchQuery,
        };

        static bool MatchesSearch(string groupName, string? searchQuery) =>
            string.IsNullOrWhiteSpace(searchQuery) ||
            groupName.Contains(searchQuery.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

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

    public static GroupSummaryItem FromDto(GroupSummaryDto dto, bool isCurrentUserMember) => new()
    {
        GroupId = dto.GroupId,
        Name = dto.Name,
        Description = dto.Description,
        Visibility = dto.Visibility.ToString(),
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
        ActiveMembers = dto.ActiveMemberCount,
        IsPublic = dto.Visibility == GroupVisibility.Public,
        IsCurrentUserMember = true,
    };
}

// ── Create ───────────────────────────────────────────────────────────────────

public sealed class GroupCreateViewModel
{
    [Required(ErrorMessage = "Group name is required.")]
    [MinLength(3, ErrorMessage = "Name must be at least 3 characters.")]
    [MaxLength(80, ErrorMessage = "Name cannot exceed 80 characters.")]
    [Display(Name = "Group Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
    [Display(Name = "Group Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose a visibility setting.")]
    [Display(Name = "Privacy")]
    public GroupVisibility? Visibility { get; set; }

    public CreateGroupRequest ToRequest() => new()
    {
        Name = Name,
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
        Visibility = Visibility,
    };
}

// ── Manage (Detail) ──────────────────────────────────────────────────────────

/// <summary>
/// Page model for the group management / detail page.
/// </summary>
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

    // Edit sub-form — pre-populated for the owner settings panel
    public string? EditName { get; set; }
    public string? EditDescription { get; set; }
    public GroupVisibility? EditVisibility { get; set; }
    public GroupWallpaperTheme? EditWallpaperTheme { get; set; }

    // Invite sub-form
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

public sealed class GroupAnnouncementItem
{
    public Guid AnnouncementId { get; init; }
    public Guid GroupId { get; init; }
    public string AuthorDisplayName { get; init; } = string.Empty;
    public string AuthorUsername { get; init; } = string.Empty;
    public GroupAnnouncementType AnnouncementType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public Guid? RelatedEventId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public bool IsEventAnnouncement => AnnouncementType == GroupAnnouncementType.EventCreated;
    public string TypeLabel => IsEventAnnouncement ? "Event update" : "Owner post";
    public string CreatedLabel => CreatedAtUtc.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture);

    public static GroupAnnouncementItem FromDto(GroupAnnouncementDto dto) => new()
    {
        AnnouncementId = dto.AnnouncementId,
        GroupId = dto.GroupId,
        AuthorDisplayName = dto.AuthorDisplayName,
        AuthorUsername = dto.AuthorUsername,
        AnnouncementType = dto.AnnouncementType,
        Title = dto.Title,
        Body = dto.Body,
        RelatedEventId = dto.RelatedEventId,
        CreatedAtUtc = dto.CreatedAtUtc,
    };
}

public sealed record GroupWallpaperOption(GroupWallpaperTheme Value, string Label, string Description);

file static class GroupWallpaperOptions
{
    public static IReadOnlyList<GroupWallpaperOption> All { get; } =
    [
        new(GroupWallpaperTheme.Default, "TasteBudz Warm", "Soft neutral cards with a warm table glow."),
        new(GroupWallpaperTheme.PizzaNight, "Pizza Night", "Tomato, basil, and oven-baked energy."),
        new(GroupWallpaperTheme.SushiBar, "Sushi Bar", "Clean rice-paper texture with seaweed green."),
        new(GroupWallpaperTheme.TacoTable, "Taco Table", "Corn, lime, and salsa colors for casual meetups."),
        new(GroupWallpaperTheme.CoffeeBrunch, "Coffee Brunch", "Cafe tones for morning plans and pastries."),
        new(GroupWallpaperTheme.GardenFresh, "Garden Fresh", "Herb and market greens for lighter meals."),
    ];
}

public sealed class GroupMemberItem
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public SocialGoal? SocialGoal { get; init; }
    public string? HomeAreaZipCode { get; init; }
    public Guid? AvatarMediaAssetId { get; init; }
    public IReadOnlyCollection<string> CuisineTags { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> DietaryFlags { get; init; } = Array.Empty<string>();
    public bool IsOwner { get; init; }
    public DateTimeOffset JoinedAtUtc { get; init; }

    public string Initial => GroupCardFormatting.GetInitial(DisplayName);

    public string? AvatarUrl => GroupCardFormatting.ToMediaUrl(AvatarMediaAssetId);

    public string? GoalLabel => GroupCardFormatting.GetSocialGoalLabel(SocialGoal);

    public string? ZipLabel => GroupCardFormatting.GetZipLabel(HomeAreaZipCode);

    public string RoleLabel => IsOwner ? "Owner" : "Member";

    public string JoinedChipLabel => $"Joined {JoinedAtUtc.ToLocalTime().ToString("MMM d", CultureInfo.InvariantCulture)}";

    public string JoinedLabel => $"Joined {JoinedAtUtc.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}";

    public IReadOnlyList<string> PublicFoodTags => GroupCardFormatting.BuildPublicFoodTags(CuisineTags, DietaryFlags);

    public IReadOnlyList<string> PreviewFoodTags => PublicFoodTags.Take(6).ToArray();

    public int HiddenFoodTagCount => Math.Max(0, PublicFoodTags.Count - PreviewFoodTags.Count);

    public string PersonalityText => GroupCardFormatting.GetPersonalityText(Bio);

    public string GoalsText => GroupCardFormatting.GetSocialGoalDescription(SocialGoal);

    public static GroupMemberItem FromDto(GroupMemberDto dto, Guid ownerUserId) => new()
    {
        UserId = dto.UserId,
        DisplayName = dto.DisplayName,
        Username = dto.Username,
        Bio = dto.Bio,
        SocialGoal = dto.SocialGoal,
        HomeAreaZipCode = dto.HomeAreaZipCode,
        AvatarMediaAssetId = dto.AvatarMediaAssetId,
        CuisineTags = dto.CuisineTags,
        DietaryFlags = dto.DietaryFlags,
        IsOwner = dto.UserId == ownerUserId,
        JoinedAtUtc = dto.JoinedAtUtc,
    };
}

public sealed class GroupEventHistoryItem
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset EventStartAtUtc { get; init; }
    public int Capacity { get; init; }
    public int ActiveParticipants { get; init; }
    public string? CuisineTarget { get; init; }
    public IReadOnlyList<EventFeedbackItem> Feedback { get; init; } = [];
    public double? AverageRating { get; init; }
    public bool IsCompleted { get; init; }
    public string EventDateLabel => EventStartAtUtc.ToLocalTime().ToString("ddd, MMM d", CultureInfo.InvariantCulture);
    public string EventTimeLabel => EventStartAtUtc.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture);
    public string ParticipationLabel => $"{ActiveParticipants} / {Capacity} joined";
    public string EventAccessLabel => string.Equals(EventType, nameof(TasteBudz.Backend.Domain.EventType.Closed), StringComparison.OrdinalIgnoreCase)
        ? "Private event"
        : "Public event";
    public string EventStatusLabel => Status;
    public bool IsHistory => Status is nameof(EventStatus.Completed) or nameof(EventStatus.Cancelled);
    public bool IsPlanned => !IsHistory;
    public bool IsCancelled => Status is nameof(EventStatus.Cancelled);
    public string? AverageRatingLabel => AverageRating.HasValue
        ? $"{AverageRating.Value.ToString("0.0", CultureInfo.InvariantCulture)} / 5 average"
        : null;

    public static GroupEventHistoryItem FromDto(
        EventSummaryDto dto,
        IReadOnlyCollection<EventFeedbackDto> feedback,
        Guid currentUserId)
    {
        var feedbackItems = feedback
            .Select(item => EventFeedbackItem.FromDto(item, currentUserId))
            .ToList();

        return new()
        {
            EventId = dto.EventId,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "Untitled Event" : dto.Title,
            EventType = dto.EventType.ToString(),
            Status = dto.Status.ToString(),
            EventStartAtUtc = dto.EventStartAtUtc,
            Capacity = dto.Capacity,
            ActiveParticipants = dto.ActiveParticipants,
            CuisineTarget = dto.CuisineTarget,
            Feedback = feedbackItems,
            AverageRating = feedbackItems.Count == 0 ? null : feedbackItems.Average(item => item.Rating),
            IsCompleted = dto.Status == EventStatus.Completed,
        };
    }
}

file static class GroupCardFormatting
{
    public static string GetInitial(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "?"
            : value.Trim()[0].ToString().ToUpperInvariant();

    public static string? ToMediaUrl(Guid? mediaAssetId) =>
        mediaAssetId.HasValue
            ? $"/api/v1/media/{mediaAssetId.Value}"
            : null;

    public static string? GetSocialGoalLabel(SocialGoal? socialGoal) => socialGoal switch
    {
        SocialGoal.Friends => "Friends",
        SocialGoal.Dating => "Dating",
        SocialGoal.Networking => "Networking",
        _ => null,
    };

    public static string? GetZipLabel(string? homeAreaZipCode) =>
        string.IsNullOrWhiteSpace(homeAreaZipCode)
            ? null
            : $"ZIP {homeAreaZipCode}";

    public static IReadOnlyList<string> BuildPublicFoodTags(
        IEnumerable<string>? cuisineTags,
        IEnumerable<string>? dietaryFlags)
    {
        var tags = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddRange(cuisineTags, seen, tags);
        AddRange(dietaryFlags, seen, tags);

        return tags;
    }

    public static string GetPersonalityText(string? bio) =>
        string.IsNullOrWhiteSpace(bio)
            ? "No personality note yet."
            : Truncate(bio, 140);

    public static string GetSocialGoalDescription(SocialGoal? socialGoal) => socialGoal switch
    {
        SocialGoal.Friends => "Looking for new foodie friends",
        SocialGoal.Dating => "Open to dinner dates",
        SocialGoal.Networking => "Interested in local networking over food",
        _ => "Open to new TasteBudz connections",
    };

    public static string Truncate(string value, int maxLength)
    {
        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return $"{trimmed[..Math.Max(0, maxLength - 1)].TrimEnd()}...";
    }

    private static void AddRange(
        IEnumerable<string>? values,
        ISet<string> seen,
        ICollection<string> destination)
    {
        if (values is null)
        {
            return;
        }

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalized = value.Trim();
            if (seen.Add(normalized))
            {
                destination.Add(normalized);
            }
        }
    }
}
