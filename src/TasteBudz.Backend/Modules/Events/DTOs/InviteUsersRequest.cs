using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Events;

public sealed class InviteUsersRequest
{
    [Required]
    public IReadOnlyCollection<string> Usernames { get; init; } = Array.Empty<string>();
}
