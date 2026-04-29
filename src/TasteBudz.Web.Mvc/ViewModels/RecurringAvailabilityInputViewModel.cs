using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


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
