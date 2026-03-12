using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed class UpsertOneOffAvailabilityWindowRequest
{
    [Required]
    public DateTimeOffset? StartsAtUtc { get; init; }

    [Required]
    public DateTimeOffset? EndsAtUtc { get; init; }

    [MaxLength(100)]
    public string? Label { get; init; }
}
