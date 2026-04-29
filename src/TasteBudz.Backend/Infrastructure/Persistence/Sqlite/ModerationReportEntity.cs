namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class ModerationReportEntity
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public Domain.ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public Guid? RelatedEventId { get; set; }
    public Guid? RelatedUserId { get; set; }
    public Guid? RelatedMessageId { get; set; }
    public Domain.ModerationReportStatus Status { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolutionDecision { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
