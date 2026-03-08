// Records used by discovery swipes and Budz matching.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// The latest swipe decision one user has recorded about another.
/// </summary>
public sealed record SwipeDecision(
    Guid ActorUserId,
    Guid SubjectUserId,
    SwipeDecisionType Decision,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// The relationship record created when two users mutually like each other.
/// </summary>
public sealed record BudConnection(
    Guid Id,
    Guid UserOneId,
    Guid UserTwoId,
    BudConnectionState State,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? EndedAtUtc);