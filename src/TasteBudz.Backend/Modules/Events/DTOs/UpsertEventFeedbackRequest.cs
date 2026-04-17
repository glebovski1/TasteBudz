using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Events;

public sealed class UpsertEventFeedbackRequest
{
    [Required]
    [Range(1, 5)]
    public int? Rating { get; init; }

    [Required]
    [MaxLength(1000)]
    public string Text { get; init; } = string.Empty;
}
