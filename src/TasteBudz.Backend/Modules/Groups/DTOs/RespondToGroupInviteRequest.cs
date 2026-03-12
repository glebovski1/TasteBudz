using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed class RespondToGroupInviteRequest
{
    [Required]
    public GroupInviteStatus? Status { get; init; }
}
