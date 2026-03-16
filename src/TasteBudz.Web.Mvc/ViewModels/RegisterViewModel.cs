using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class RegisterViewModel
{
    [Required]
    [MinLength(3)]
    [MaxLength(32)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords must match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9]{5}$", ErrorMessage = "ZIP code must be a 5-digit value.")]
    [Display(Name = "ZIP Code")]
    public string ZipCode { get; set; } = string.Empty;
}
