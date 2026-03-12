namespace TasteBudz.Backend.Modules.Profiles;

public sealed record RecurringAvailabilityWindowDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Label);
