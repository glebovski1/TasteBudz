using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Auth;

public sealed class CreatePasswordResetRequestRequest
{
    [Required]
    [MaxLength(80)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; init; } = string.Empty;
}
