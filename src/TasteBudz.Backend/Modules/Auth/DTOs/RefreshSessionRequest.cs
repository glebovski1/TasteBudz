using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Auth;

public sealed class RefreshSessionRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
