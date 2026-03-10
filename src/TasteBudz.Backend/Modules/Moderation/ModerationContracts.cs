// Request and response contracts for reporting, moderation, restrictions, and audit APIs.
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Moderation;

/// <summary>
/// Moderation report payload returned by report and moderation endpoints.
/// </summary>
public sealed record ModerationReportDto(
    Guid ReportId,
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
/// Request body for submitting a moderation report.
/// </summary>
public sealed class CreateModerationReportRequest
{
    [Required]
    public ReportTargetType? TargetType { get; init; }

    [Required]
    public Guid? TargetId { get; init; }

    [Required]
    [MaxLength(80)]
    public string Category { get; init; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Reason { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Explanation { get; init; }

    public Guid? RelatedEventId { get; init; }

    public Guid? RelatedUserId { get; init; }

    public Guid? RelatedMessageId { get; init; }
}

/// <summary>
/// Query parameters for the moderation report queue.
/// </summary>
public sealed class BrowseModerationReportsQuery
{
    public ModerationReportStatus? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Request body for resolving a moderation report.
/// </summary>
public sealed class ResolveModerationReportRequest
{
    [Required]
    [MaxLength(80)]
    public string Decision { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; init; }
}

/// <summary>
/// Restriction payload returned by moderation endpoints.
/// </summary>
public sealed record RestrictionDto(
    Guid RestrictionId,
    Guid SubjectUserId,
    RestrictionScope Scope,
    string Reason,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    RestrictionStatus Status,
    DateTimeOffset? RevokedAtUtc);

/// <summary>
/// Request body for creating one scoped user restriction.
/// </summary>
public sealed class CreateRestrictionRequest
{
    [Required]
    public Guid? SubjectUserId { get; init; }

    [Required]
    public RestrictionScope? Scope { get; init; }

    [Required]
    [MaxLength(250)]
    public string Reason { get; init; } = string.Empty;

    public DateTimeOffset? ExpiresAtUtc { get; init; }
}

/// <summary>
/// Request body for restriction updates or revocation.
/// </summary>
public sealed class UpdateRestrictionRequest
{
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    public bool? Revoke { get; init; }

    [MaxLength(250)]
    public string? Reason { get; init; }
}

/// <summary>
/// Query parameters for audit-log search.
/// </summary>
public sealed class AuditLogQuery
{
    public Guid? ActorUserId { get; init; }

    public string? TargetEntityType { get; init; }

    public Guid? TargetEntityId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Audit-log payload returned by the admin-only audit API.
/// </summary>
public sealed record AuditLogEntryDto(
    Guid AuditLogEntryId,
    string ActionType,
    Guid ActorUserId,
    string TargetEntityType,
    Guid? TargetEntityId,
    DateTimeOffset CreatedAtUtc,
    string Details);
