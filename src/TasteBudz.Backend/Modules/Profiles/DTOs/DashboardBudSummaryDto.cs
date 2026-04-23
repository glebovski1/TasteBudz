using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed record DashboardBudSummaryDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? Bio,
    SocialGoal? SocialGoal,
    string? HomeAreaZipCode,
    Guid? AvatarMediaAssetId,
    IReadOnlyCollection<string> CuisineTags,
    IReadOnlyCollection<string> DietaryFlags,
    DateTimeOffset ConnectedAtUtc);
