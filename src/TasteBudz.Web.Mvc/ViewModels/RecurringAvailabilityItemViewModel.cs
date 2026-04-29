using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


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
