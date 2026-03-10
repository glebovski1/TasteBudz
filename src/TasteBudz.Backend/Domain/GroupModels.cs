// Group aggregate and membership records.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// Top-level group record owned by a single user.
/// </summary>
public sealed record Group(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    GroupLifecycleState LifecycleState,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// Membership state for a user in a group.
/// </summary>
public sealed record GroupMember(
    Guid GroupId,
    Guid UserId,
    GroupMemberState State,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// Invite record used to track pending or completed group invitations.
/// </summary>
public sealed record GroupInvite(
    Guid Id,
    Guid GroupId,
    Guid InvitedUserId,
    Guid InviterUserId,
    GroupInviteStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);