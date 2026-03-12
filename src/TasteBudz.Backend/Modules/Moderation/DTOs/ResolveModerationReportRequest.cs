using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Moderation;

public sealed class ResolveModerationReportRequest
{
    [Required]
    [MaxLength(80)]
    public string Decision { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; init; }
}
