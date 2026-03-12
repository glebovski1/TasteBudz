using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Discovery;

public sealed record DiscoveryProfilePreviewDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? Bio,
    SocialGoal? SocialGoal);
