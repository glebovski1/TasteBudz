using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed class UpdateGroupRequest
{
    [MinLength(3)]
    [MaxLength(80)]
    public string? Name { get; init; }

    [MaxLength(250)]
    public string? Description { get; init; }

    public GroupVisibility? Visibility { get; init; }
}
