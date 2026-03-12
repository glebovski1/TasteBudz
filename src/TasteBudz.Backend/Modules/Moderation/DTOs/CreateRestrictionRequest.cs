using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Moderation;

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
