using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Moderation;

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
