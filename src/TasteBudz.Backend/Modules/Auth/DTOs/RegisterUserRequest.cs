using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Auth;

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
