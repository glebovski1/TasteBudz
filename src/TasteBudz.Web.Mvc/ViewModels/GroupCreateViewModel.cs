using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class GroupCreateViewModel
{
    [Required(ErrorMessage = "Group name is required.")]
    [MinLength(3, ErrorMessage = "Name must be at least 3 characters.")]
    [MaxLength(80, ErrorMessage = "Name cannot exceed 80 characters.")]
    [Display(Name = "Group Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
    [Display(Name = "Group Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose a visibility setting.")]
    [Display(Name = "Privacy")]
    public GroupVisibility? Visibility { get; set; }

    public CreateGroupRequest ToRequest() => new()
    {
        Name = Name,
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
        Visibility = Visibility,
    };
}
