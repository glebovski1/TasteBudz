using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Events;

public sealed class CancelEventRequest
{
    [Required]
    [MaxLength(250)]
    public string Reason { get; init; } = string.Empty;
}
