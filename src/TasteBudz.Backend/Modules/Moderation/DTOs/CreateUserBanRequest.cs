using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Moderation;

public sealed class CreateUserBanRequest
{
    [Required]
    public Guid? SubjectUserId { get; init; }

    [Required]
    [MaxLength(250)]
    public string Reason { get; init; } = string.Empty;

    public DateTimeOffset? ExpiresAtUtc { get; init; }

    public Guid? ReportId { get; init; }
}
