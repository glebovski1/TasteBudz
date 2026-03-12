using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Moderation;

public sealed class UpdateRestrictionRequest
{
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    public bool? Revoke { get; init; }

    [MaxLength(250)]
    public string? Reason { get; init; }
}
