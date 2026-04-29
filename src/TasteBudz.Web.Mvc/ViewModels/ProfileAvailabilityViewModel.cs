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
