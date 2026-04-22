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

/// <summary>
/// One-time credential reset token issued by an admin.
/// </summary>
public sealed record PasswordResetToken(
    Guid Id,
    Guid UserId,
    string TokenHash,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? UsedAtUtc,
    DateTimeOffset? RevokedAtUtc);

/// <summary>
/// Admin-reviewed password reset request submitted by a user.
/// </summary>
public sealed record PasswordResetRequest(
    Guid Id,
    string Username,
    string Message,
    Guid? MatchedUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    Guid? ClosedByUserId);
