// Moderation, restriction, and audit records.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// User-submitted moderation report with one canonical target.
/// </summary>
public sealed record ModerationReport(
    Guid Id,
    Guid ReporterUserId,
    ReportTargetType TargetType,
    Guid TargetId,
    string Category,
    string Reason,
    string? Explanation,
    Guid? RelatedEventId,
    Guid? RelatedUserId,
    Guid? RelatedMessageId,
    DateTimeOffset CreatedAtUtc,
    ModerationReportStatus Status,
    Guid? ResolvedByUserId,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolutionDecision,
    string? ResolutionNotes);

/// <summary>
/// Explicit moderation action stored for traceability.
/// </summary>
public sealed record ModerationAction(
    Guid Id,
    Guid ActorUserId,
    Guid? ReportId,
    ModerationActionType ActionType,
    string Notes,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Scoped restriction applied to one user.
/// </summary>
public sealed record UserRestriction(
    Guid Id,
    Guid SubjectUserId,
    Guid IssuedByUserId,
    RestrictionScope Scope,
    string Reason,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    RestrictionStatus Status,
    DateTimeOffset? RevokedAtUtc);

/// <summary>
/// Append-only audit record for sensitive actions.
/// </summary>
public sealed record AuditLogEntry(
    Guid Id,
    string ActionType,
    Guid ActorUserId,
    string TargetEntityType,
    Guid? TargetEntityId,
    DateTimeOffset CreatedAtUtc,
    string Details);
