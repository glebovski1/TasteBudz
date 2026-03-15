using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Auth;

public sealed record CurrentUserSummaryDto(
    Guid UserId,
    string Username,
    string Email,
    IReadOnlyCollection<UserRole> Roles);
