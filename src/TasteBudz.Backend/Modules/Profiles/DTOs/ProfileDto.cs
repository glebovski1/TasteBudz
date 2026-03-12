using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed record ProfileDto(
    Guid UserId,
    string Username,
    string Email,
    string DisplayName,
    string? Bio,
    string HomeAreaZipCode,
    SocialGoal? SocialGoal);
