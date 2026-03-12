namespace TasteBudz.Backend.Modules.Profiles;

public sealed record DashboardDto(
    ProfileDto Profile,
    IReadOnlyCollection<DashboardEventSummaryDto> ActiveEvents,
    IReadOnlyCollection<DashboardGroupSummaryDto> ActiveGroups,
    IReadOnlyCollection<DashboardBudSummaryDto> Budz);
