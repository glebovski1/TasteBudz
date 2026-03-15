using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Auth;

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
