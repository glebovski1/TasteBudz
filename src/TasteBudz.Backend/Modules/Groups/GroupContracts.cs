// Request and response contracts for group browse and membership workflows.
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;

namespace TasteBudz.Backend.Modules.Groups;

/// <summary>
/// Group card returned by browse endpoints.
/// </summary>
public sealed record GroupSummaryDto(
    Guid GroupId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    int ActiveMembers);

/// <summary>
/// Full group payload returned by the detail endpoint.
/// </summary>
public sealed record GroupDetailDto(
    Guid GroupId,
    Guid OwnerUserId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    GroupLifecycleState LifecycleState,
    bool IsCurrentUserMember,
    IReadOnlyCollection<GroupMemberDto> Members);

/// <summary>
/// Group member summary used inside detail responses.
/// </summary>
public sealed record GroupMemberDto(
    Guid UserId,
    string Username,
    string DisplayName,
    GroupMemberState State,
    DateTimeOffset JoinedAtUtc);

/// <summary>
/// Group invite payload returned by invite workflows.
/// </summary>
public sealed record GroupInviteDto(
    Guid InviteId,
    Guid GroupId,
    Guid InvitedUserId,
    string InvitedUsername,
    GroupInviteStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// Query parameters for public-group browse/search.
/// </summary>
public sealed class BrowseGroupsQuery
{
    public string? Q { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Query parameters for group-linked event listing.
/// </summary>
public sealed class GroupEventsQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Request body for group creation.
/// </summary>
public sealed class CreateGroupRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(80)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; init; }

    [Required]
    public GroupVisibility? Visibility { get; init; }
}

/// <summary>
/// Partial update payload for owner-managed group settings.
/// </summary>
public sealed class UpdateGroupRequest
{
    [MinLength(3)]
    [MaxLength(80)]
    public string? Name { get; init; }

    [MaxLength(250)]
    public string? Description { get; init; }

    public GroupVisibility? Visibility { get; init; }
}

/// <summary>
/// Request body for inviting one user to a private group.
/// </summary>
public sealed class InviteUserToGroupRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;
}

/// <summary>
/// Request body for invite acceptance/decline.
/// </summary>
public sealed class RespondToGroupInviteRequest
{
    [Required]
    public GroupInviteStatus? Status { get; init; }
}
