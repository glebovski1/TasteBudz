namespace TasteBudz.Backend.Modules.Profiles;

public sealed record DashboardDto(
    ProfileDto Profile,
    IReadOnlyCollection<DashboardEventSummaryDto> MyEvents,
    IReadOnlyCollection<DashboardGroupSummaryDto> ActiveGroups,
    IReadOnlyCollection<DashboardBudSummaryDto> Budz);
