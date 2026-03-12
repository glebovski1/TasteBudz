using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

public sealed class CreateEventRequest
{
    [Required]
    public EventType? EventType { get; init; }

    [Required]
    public DateTimeOffset? EventStartAtUtc { get; init; }

    [Required]
    [Range(2, 8)]
    public int? Capacity { get; init; }

    [MaxLength(120)]
    public string? Title { get; init; }

    public Guid? SelectedRestaurantId { get; init; }

    [MaxLength(120)]
    public string? CuisineTarget { get; init; }

    public Guid? GroupId { get; init; }

    public IReadOnlyCollection<string> InviteUsernames { get; init; } = Array.Empty<string>();
}
