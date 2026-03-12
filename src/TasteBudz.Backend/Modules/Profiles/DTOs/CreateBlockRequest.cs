using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed class CreateBlockRequest
{
    [Required]
    public Guid? BlockedUserId { get; init; }
}
