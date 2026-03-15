using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Auth;

/// <summary>
/// Request body for access-token renewal.
/// </summary>
public sealed class RefreshSessionRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
