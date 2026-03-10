// Represents the authenticated caller after the bearer token has been translated into claims.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Infrastructure.Auth;

/// <summary>
/// Lightweight user identity that controllers and services use after authentication succeeds.
/// </summary>
public sealed record CurrentUser(
    Guid UserId,
    string Username,
    IReadOnlyCollection<UserRole> Roles)
{
    /// <summary>
    /// Convenience helper for permission checks that stay inside the application layer.
    /// </summary>
    public bool IsInRole(UserRole role) => Roles.Contains(role);
}