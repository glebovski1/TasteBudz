using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class EventIndexViewModel
{
    public const int DefaultRadiusMiles = 10;
    public static IReadOnlyList<int> AvailableRadiusMiles { get; } = [5, 10, 25];

    public IReadOnlyList<EventSummaryItem> Events { get; init; } = [];
    public bool IsQuickSearch { get; init; }
    public string? SearchQuery { get; init; }
    public bool UseMyZip { get; init; }
    public int RadiusMiles { get; init; } = DefaultRadiusMiles;
    public bool AvailabilityOnly { get; init; }
    public EventStatus? SelectedStatus { get; init; }
    public EventType? SelectedEventType { get; init; }
    public string EventScope { get; init; } = EventScopeAll;
    public string? HomeAreaZipCode { get; init; }
    public bool ShowAvailabilitySetupCta { get; init; }
    public string? RecommendationSummary { get; init; }
    public const string EventScopeAll = "all";
    public const string EventScopeGroup = "group";
    public const string EventScopeOrdinary = "ordinary";
    public static IReadOnlyList<(EventStatus? Value, string Label)> AvailableStatusFilters { get; } =
    [
        (null, "All statuses"),
        (EventStatus.Open, "Active/Open"),
        (EventStatus.Full, "Full events"),
        (EventStatus.Confirmed, "Confirmed events"),
        (EventStatus.Completed, "Completed events"),
        (EventStatus.Cancelled, "Cancelled events"),
    ];
    public static IReadOnlyList<(EventType? Value, string Label)> AvailableEventTypeFilters { get; } =
    [
        (null, "All event types"),
        (EventType.Open, "Open events"),
        (EventType.Closed, "Closed events"),
    ];

    public static EventIndexViewModel Empty => new();

    public static EventIndexViewModel EmptyWithFilters(
        string? searchQuery,
        bool useMyZip,
        int radiusMiles,
        bool availabilityOnly,
        EventStatus? selectedStatus,
        EventType? selectedEventType,
        string? eventScope,
        string? homeAreaZipCode,
        bool showAvailabilitySetupCta = false,
        bool isQuickSearch = false,
        string? recommendationSummary = null) =>
        new()
        {
            IsQuickSearch = isQuickSearch,
            SearchQuery = searchQuery,
            UseMyZip = useMyZip,
            RadiusMiles = radiusMiles,
            AvailabilityOnly = availabilityOnly,
            SelectedStatus = selectedStatus,
            SelectedEventType = selectedEventType,
            EventScope = NormalizeEventScope(eventScope),
            HomeAreaZipCode = homeAreaZipCode,
            ShowAvailabilitySetupCta = showAvailabilitySetupCta,
            RecommendationSummary = recommendationSummary,
        };

    public static EventIndexViewModel FromDto(
        IEnumerable<EventSummaryDto> events,
        string? searchQuery = null,
        bool useMyZip = false,
        int radiusMiles = DefaultRadiusMiles,
        bool availabilityOnly = false,
        EventStatus? selectedStatus = null,
        EventType? selectedEventType = null,
        string? eventScope = null,
        string? homeAreaZipCode = null,
        bool showAvailabilitySetupCta = false,
        bool isQuickSearch = false,
        string? recommendationSummary = null) => new()
        {
            Events = events.Select(EventSummaryItem.FromDto).ToList(),
            IsQuickSearch = isQuickSearch,
            SearchQuery = searchQuery,
            UseMyZip = useMyZip,
            RadiusMiles = radiusMiles,
            AvailabilityOnly = availabilityOnly,
            SelectedStatus = selectedStatus,
            SelectedEventType = selectedEventType,
            EventScope = NormalizeEventScope(eventScope),
            HomeAreaZipCode = homeAreaZipCode,
            ShowAvailabilitySetupCta = showAvailabilitySetupCta,
            RecommendationSummary = recommendationSummary,
        };

    public static bool? ToGroupLinkedFilter(string? eventScope) => NormalizeEventScope(eventScope) switch
    {
        EventScopeGroup => true,
        EventScopeOrdinary => false,
        _ => null,
    };

    private static string NormalizeEventScope(string? eventScope) =>
        string.Equals(eventScope, EventScopeGroup, StringComparison.OrdinalIgnoreCase)
            ? EventScopeGroup
            : string.Equals(eventScope, EventScopeOrdinary, StringComparison.OrdinalIgnoreCase)
                ? EventScopeOrdinary
                : EventScopeAll;
}
