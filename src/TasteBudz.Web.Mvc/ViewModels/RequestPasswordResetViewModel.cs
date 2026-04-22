using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class RequestPasswordResetViewModel
{
    [Required]
    [MaxLength(80)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [DataType(DataType.MultilineText)]
    public string Message { get; set; } = string.Empty;
}
