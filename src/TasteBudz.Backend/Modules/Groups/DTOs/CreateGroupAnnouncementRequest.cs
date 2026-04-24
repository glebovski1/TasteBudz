using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Groups;

public sealed class CreateGroupAnnouncementRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(120)]
    public string? Title { get; init; }

    [Required]
    [MinLength(3)]
    [MaxLength(1000)]
    public string? Body { get; init; }
}
