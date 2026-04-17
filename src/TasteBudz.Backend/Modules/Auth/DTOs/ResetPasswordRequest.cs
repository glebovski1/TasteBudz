using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Auth;

public sealed class ResetPasswordRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}
