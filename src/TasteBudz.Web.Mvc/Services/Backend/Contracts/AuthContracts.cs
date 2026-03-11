using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Web.Mvc.Services.Backend.Contracts;

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

public sealed class LoginRequest
{
    [Required]
    public string UsernameOrEmail { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshSessionRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record CurrentUserSummaryDto(
    Guid UserId,
    string Username,
    string Email,
    IReadOnlyCollection<UserRole> Roles);

public sealed record SessionDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    CurrentUserSummaryDto CurrentUser);
