using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class LoginViewModel
{
    [Required]
    [Display(Name = "Username or Email")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
