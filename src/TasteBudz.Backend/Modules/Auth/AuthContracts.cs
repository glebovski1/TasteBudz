// Request and response contracts for session-based authentication endpoints.
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Auth;

/// <summary>
/// Request body for account registration.
/// </summary>
public sealed class RegisterUserRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(32)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9]{5}$")]
    public string ZipCode { get; init; } = string.Empty;
}

/// <summary>
/// Request body for username/email plus password login.
/// </summary>
public sealed class LoginRequest
{
    [Required]
    public string UsernameOrEmail { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Request body for access-token renewal.
/// </summary>
public sealed class RefreshSessionRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Small caller snapshot returned together with a session token pair.
/// </summary>
public sealed record CurrentUserSummaryDto(
    Guid UserId,
    string Username,
    string Email,
    IReadOnlyCollection<UserRole> Roles);

/// <summary>
/// Auth response containing both tokens and the current-user summary needed by the client.
/// </summary>
public sealed record SessionDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    CurrentUserSummaryDto CurrentUser);