using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Groups;

public sealed class InviteUserToGroupRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;
}
