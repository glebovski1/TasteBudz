// Core authentication and session records.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// Persisted account identity and status information.
/// </summary>
public sealed record UserAccount(
    Guid Id,
    string Username,
    string NormalizedUsername,
    string Email,
    string NormalizedEmail,
    string PasswordHash,
    AccountStatus Status,
    IReadOnlyCollection<UserRole> Roles,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeletedAtUtc);

/// <summary>
/// Opaque access/refresh token pair tracked by the backend.
/// </summary>
public sealed record UserSession(
    Guid Id,
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset RefreshExpiresAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? RevokedAtUtc);