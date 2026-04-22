using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class ProfileAvailabilityViewModel
{
    public static IReadOnlyList<SelectListItem> DayOptions { get; } =
        Enum.GetValues<DayOfWeek>()
            .Select(day => new SelectListItem(day.ToString(), day.ToString()))
            .ToArray();

    public IReadOnlyCollection<RecurringAvailabilityItemViewModel> RecurringWindows { get; init; } = [];

    public IReadOnlyCollection<OneOffAvailabilityItemViewModel> OneOffWindows { get; init; } = [];

    public static ProfileAvailabilityViewModel FromDto(
        IEnumerable<RecurringAvailabilityWindowDto> recurring,
        IEnumerable<OneOffAvailabilityWindowDto> oneOff) =>
        new()
        {
            RecurringWindows = recurring
                .OrderBy(window => window.DayOfWeek)
                .ThenBy(window => window.StartTime)
                .Select(RecurringAvailabilityItemViewModel.FromDto)
                .ToArray(),
            OneOffWindows = oneOff
                .OrderBy(window => window.StartsAtUtc)
                .Select(OneOffAvailabilityItemViewModel.FromDto)
                .ToArray(),
        };
}

public sealed class RecurringAvailabilityInputViewModel
{
    public Guid? WindowId { get; set; }

    [Required]
    [Display(Name = "Day")]
    public DayOfWeek? DayOfWeek { get; set; }

    [Required]
    [Display(Name = "Start")]
    public TimeOnly? StartTime { get; set; }

    [Required]
    [Display(Name = "End")]
    public TimeOnly? EndTime { get; set; }

    [MaxLength(100)]
    public string? Label { get; set; }

    public UpsertRecurringAvailabilityWindowRequest ToRequest() =>
        new()
        {
            DayOfWeek = DayOfWeek,
            StartTime = StartTime,
            EndTime = EndTime,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label.Trim(),
        };
}

public sealed class OneOffAvailabilityInputViewModel
{
    public Guid? WindowId { get; set; }

    [Required]
    [Display(Name = "Starts")]
    public DateTime? StartsAt { get; set; }

    [Required]
    [Display(Name = "Ends")]
    public DateTime? EndsAt { get; set; }

    [MaxLength(100)]
    public string? Label { get; set; }

    public UpsertOneOffAvailabilityWindowRequest ToRequest() =>
        new()
        {
            StartsAtUtc = StartsAt.HasValue ? new DateTimeOffset(StartsAt.Value, TimeSpan.Zero) : null,
            EndsAtUtc = EndsAt.HasValue ? new DateTimeOffset(EndsAt.Value, TimeSpan.Zero) : null,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label.Trim(),
        };
}

public sealed class RecurringAvailabilityItemViewModel
{
    public Guid Id { get; init; }

    public DayOfWeek DayOfWeek { get; init; }

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }

    public string? Label { get; init; }

    public static RecurringAvailabilityItemViewModel FromDto(RecurringAvailabilityWindowDto dto) =>
        new()
        {
            Id = dto.Id,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Label = dto.Label,
        };
}

public sealed class OneOffAvailabilityItemViewModel
{
    public Guid Id { get; init; }

    public DateTimeOffset StartsAtUtc { get; init; }

    public DateTimeOffset EndsAtUtc { get; init; }

    public string? Label { get; init; }

    public DateTime StartsAtInput => StartsAtUtc.UtcDateTime;

    public DateTime EndsAtInput => EndsAtUtc.UtcDateTime;

    public static OneOffAvailabilityItemViewModel FromDto(OneOffAvailabilityWindowDto dto) =>
        new()
        {
            Id = dto.Id,
            StartsAtUtc = dto.StartsAtUtc,
            EndsAtUtc = dto.EndsAtUtc,
            Label = dto.Label,
        };
}
