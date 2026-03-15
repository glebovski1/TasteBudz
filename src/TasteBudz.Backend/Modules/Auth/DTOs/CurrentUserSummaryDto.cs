using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Auth;

/// <summary>
/// Small caller snapshot returned together with a session token pair.
/// </summary>
public sealed record CurrentUserSummaryDto(
    Guid UserId,
    string Username,
    string Email,
    IReadOnlyCollection<UserRole> Roles);
