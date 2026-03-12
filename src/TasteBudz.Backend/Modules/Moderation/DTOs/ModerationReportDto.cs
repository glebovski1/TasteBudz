using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Moderation;

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
