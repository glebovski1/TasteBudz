using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
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

    // Edit sub-form — pre-populated for the owner settings panel
    public string? EditName { get; set; }
    public string? EditDescription { get; set; }
    public GroupVisibility? EditVisibility { get; set; }

    // Invite sub-form
    public string? InviteUsername { get; set; }

    public static GroupManageViewModel FromDto(GroupDetailDto dto, Guid currentUserId) => new()
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
    public bool IsOwner { get; init; }
    public DateTimeOffset JoinedAtUtc { get; init; }

    public static GroupMemberItem FromDto(GroupMemberDto dto, Guid ownerUserId) => new()
    {
        UserId = dto.UserId,
        DisplayName = dto.DisplayName,
        Username = dto.Username,
        IsOwner = dto.UserId == ownerUserId,
        JoinedAtUtc = dto.JoinedAtUtc,
    };
}
