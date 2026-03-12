using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

public sealed class BrowseEventsQuery
{
    public string? Q { get; init; }

    public string? Cuisine { get; init; }

    public PriceTier? PriceTier { get; init; }

    public EventStatus? Status { get; init; }

    public EventType? EventType { get; init; }

    [RegularExpression("^[0-9]{5}$")]
    public string? ZipCode { get; init; }

    [Range(1, 100)]
    public double? RadiusMiles { get; init; }

    public DateTimeOffset? StartsAfter { get; init; }

    public DateTimeOffset? StartsBefore { get; init; }

    public bool AvailabilityOnly { get; init; }

    public Guid? GroupId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
