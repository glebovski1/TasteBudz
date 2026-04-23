using System.ComponentModel.DataAnnotations;
using System.Globalization;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;

namespace TasteBudz.Web.Mvc.ViewModels;

// ── Browse ───────────────────────────────────────────────────────────────────

/// <summary>
/// Page model for the Groups browse/index page.
/// </summary>
public sealed class GroupIndexViewModel
{
    public IReadOnlyList<GroupSummaryItem> Groups { get; init; } = [];
    public string? SearchQuery { get; init; }

    public static GroupIndexViewModel Empty => new();

    public static GroupIndexViewModel FromDto(
        IEnumerable<GroupSummaryDto> groups,
        string? searchQuery = null) => new()
        {
            Groups = groups.Select(GroupSummaryItem.FromDto).ToList(),
            SearchQuery = searchQuery,
        };
}

public sealed class GroupSummaryItem
{
    public Guid GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Visibility { get; init; } = string.Empty;
    public int ActiveMembers { get; init; }
    public bool IsPublic { get; init; }

    public static GroupSummaryItem FromDto(GroupSummaryDto dto) => new()
    {
        GroupId = dto.GroupId,
        Name = dto.Name,
        Description = dto.Description,
        Visibility = dto.Visibility.ToString(),
        ActiveMembers = dto.ActiveMembers,
        IsPublic = dto.Visibility == GroupVisibility.Public,
    };
}

// ── Create ───────────────────────────────────────────────────────────────────

/// <summary>
/// Form model for creating a new group.
/// </summary>
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
    public bool IsCurrentUserOwner { get; init; }
    public bool IsCurrentUserMember { get; init; }
    public IReadOnlyList<GroupMemberItem> Members { get; init; } = [];
    public IReadOnlyList<GroupEventHistoryItem> EventHistory { get; init; } = [];

    // Edit sub-form — pre-populated for the owner settings panel
    public string? EditName { get; set; }
    public string? EditDescription { get; set; }
    public GroupVisibility? EditVisibility { get; set; }

    // Invite sub-form
    public string? InviteUsername { get; set; }

    public static GroupManageViewModel FromDto(
        GroupDetailDto dto,
        Guid currentUserId,
        IReadOnlyList<GroupEventHistoryItem>? eventHistory = null) => new()
    {
        GroupId = dto.GroupId,
        Name = dto.Name,
        Description = dto.Description,
        Visibility = dto.Visibility.ToString(),
        IsCurrentUserOwner = dto.OwnerUserId == currentUserId,
        IsCurrentUserMember = dto.IsCurrentUserMember,
        Members = dto.Members
            .Where(m => m.State == GroupMemberState.Active)
            .Select(m => GroupMemberItem.FromDto(m, dto.OwnerUserId))
            .ToList(),
        EventHistory = eventHistory ?? [],
        EditName = dto.Name,
        EditDescription = dto.Description,
        EditVisibility = dto.Visibility,
    };
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
