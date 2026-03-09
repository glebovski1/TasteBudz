// Shared enum definitions referenced across modules.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// Application-level authorization roles.
/// </summary>
public enum UserRole
{
    User,
    Moderator,
    Admin,
}

/// <summary>
/// Lifecycle states for a user account.
/// </summary>
public enum AccountStatus
{
    Active,
    Deleted,
}

/// <summary>
/// High-level reason a user is using the app socially.
/// </summary>
public enum SocialGoal
{
    Friends,
    Dating,
    Networking,
}

/// <summary>
/// Simplified spice preference scale.
/// </summary>
public enum SpiceTolerance
{
    Mild,
    Medium,
    Hot,
}

/// <summary>
/// Coarse restaurant price buckets.
/// </summary>
public enum PriceTier
{
    One,
    Two,
    Three,
    Four,
}

/// <summary>
/// Open events allow direct joins; closed events use invitations.
/// </summary>
public enum EventType
{
    Open,
    Closed,
}

/// <summary>
/// Server-owned event lifecycle states.
/// </summary>
public enum EventStatus
{
    Open,
    Full,
    Confirmed,
    Cancelled,
    Completed,
}

/// <summary>
/// Participation states tracked per user per event.
/// </summary>
public enum EventParticipantState
{
    Invited,
    Joined,
    Declined,
    Left,
    Removed,
}

/// <summary>
/// Group visibility modes.
/// </summary>
public enum GroupVisibility
{
    Public,
    Private,
}

/// <summary>
/// Lifecycle states for a group.
/// </summary>
public enum GroupLifecycleState
{
    Active,
    Dissolved,
}

/// <summary>
/// Membership states for a user inside a group.
/// </summary>
public enum GroupMemberState
{
    Active,
    Left,
    Removed,
}

/// <summary>
/// Invite states for group membership workflows.
/// </summary>
public enum GroupInviteStatus
{
    Pending,
    Accepted,
    Declined,
    Revoked,
    Expired,
}

/// <summary>
/// Discovery decisions a user can make about another user.
/// </summary>
public enum SwipeDecisionType
{
    Like,
    Pass,
}

/// <summary>
/// Relationship state between two matched users.
/// </summary>
public enum BudConnectionState
{
    Connected,
    Removed,
}

/// <summary>
/// Notification categories currently emitted by the backend.
/// </summary>
public enum NotificationType
{
    EventInviteReceived,
    EventJoined,
    EventLeft,
    EventConfirmed,
    EventCancelled,
    EventUpdated,
    GroupInviteReceived,
    BudMatched,
}

/// <summary>
/// Supported chat scopes for the MVP messaging core.
/// </summary>
public enum ChatScopeType
{
    Event,
    Group,
    Direct,
}

/// <summary>
/// Canonical target types for moderation reports.
/// </summary>
public enum ReportTargetType
{
    User,
    Message,
}

/// <summary>
/// Review state for a moderation report.
/// </summary>
public enum ModerationReportStatus
{
    Pending,
    Resolved,
}

/// <summary>
/// Stored moderation action types.
/// </summary>
public enum ModerationActionType
{
    ReportResolved,
    RestrictionCreated,
    RestrictionUpdated,
    RestrictionRevoked,
}

/// <summary>
/// Restriction scopes enforced by MVP workflows.
/// </summary>
public enum RestrictionScope
{
    DiscoveryVisibility,
    ChatSend,
    EventJoin,
    EventCreate,
}

/// <summary>
/// Lifecycle states for a scoped user restriction.
/// </summary>
public enum RestrictionStatus
{
    Active,
    Expired,
    Revoked,
}
