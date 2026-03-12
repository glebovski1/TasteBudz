using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed class CreateGroupRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(80)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; init; }

    [Required]
    public GroupVisibility? Visibility { get; init; }
}
