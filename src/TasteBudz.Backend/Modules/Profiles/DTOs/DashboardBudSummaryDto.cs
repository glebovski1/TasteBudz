namespace TasteBudz.Backend.Modules.Profiles;

public sealed record DashboardBudSummaryDto(
    Guid UserId,
    string Username,
    string DisplayName);
