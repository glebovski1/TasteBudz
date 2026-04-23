using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed record DashboardGroupSummaryDto(
    Guid GroupId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    int ActiveMemberCount);
