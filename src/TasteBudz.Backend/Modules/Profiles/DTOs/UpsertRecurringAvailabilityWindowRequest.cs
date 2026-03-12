using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed class UpsertRecurringAvailabilityWindowRequest
{
    [Required]
    public DayOfWeek? DayOfWeek { get; init; }

    [Required]
    public TimeOnly? StartTime { get; init; }

    [Required]
    public TimeOnly? EndTime { get; init; }

    [MaxLength(100)]
    public string? Label { get; init; }
}
