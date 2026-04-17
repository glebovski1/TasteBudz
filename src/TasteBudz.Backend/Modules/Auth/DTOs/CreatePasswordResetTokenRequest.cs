using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Auth;

public sealed class CreatePasswordResetTokenRequest
{
    [Required]
    public string UsernameOrEmail { get; init; } = string.Empty;
}
