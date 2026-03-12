using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Events;

public sealed class UpdateEventRequest
{
    [MaxLength(120)]
    public string? Title { get; init; }

    public DateTimeOffset? EventStartAtUtc { get; init; }

    [Range(2, 8)]
    public int? Capacity { get; init; }

    public Guid? SelectedRestaurantId { get; init; }

    [MaxLength(120)]
    public string? CuisineTarget { get; init; }

    public Guid? GroupId { get; init; }
}
