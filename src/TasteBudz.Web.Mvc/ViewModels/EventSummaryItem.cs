using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class EventSummaryItem
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset EventStartAtUtc { get; init; }
    public int Capacity { get; init; }
    public int ActiveParticipants { get; init; }
    public string? CuisineTarget { get; init; }
    public Guid? GroupId { get; init; }
    public double? DistanceMiles { get; init; }
    public int MatchingCuisineCount { get; init; }
    public int MatchingBudzCount { get; init; }
    public bool HasActiveSlotReservation { get; init; }
    public bool IsDiscountActive { get; init; }
    public int? DiscountPercent { get; init; }

    public IReadOnlyList<string> RecommendationReasons
    {
        get
        {
            var reasons = new List<string>();

            if (DistanceMiles.HasValue)
            {
                reasons.Add($"{DistanceMiles.Value:0.#} mi away");
            }

            if (MatchingCuisineCount > 0)
            {
                reasons.Add(MatchingCuisineCount == 1
                    ? "Matches 1 food preference"
                    : $"Matches {MatchingCuisineCount} food preferences");
            }

            if (MatchingBudzCount > 0)
            {
                reasons.Add(MatchingBudzCount == 1
                    ? "1 Bud already joined"
                    : $"{MatchingBudzCount} Budz already joined");
            }

            return reasons;
        }
    }

    public bool IsGroupLinked => GroupId.HasValue;
    public string ScopeLabel => IsGroupLinked ? "Group event" : string.Empty;

    public static EventSummaryItem FromDto(EventSummaryDto dto) => new()
    {
        EventId = dto.EventId,
        Title = string.IsNullOrWhiteSpace(dto.Title) ? "Untitled Event" : dto.Title,
        EventType = dto.EventType.ToString(),
        Status = dto.Status.ToString(),
        EventStartAtUtc = dto.EventStartAtUtc,
        Capacity = dto.Capacity,
        ActiveParticipants = dto.ActiveParticipants,
        CuisineTarget = dto.CuisineTarget,
        GroupId = dto.GroupId,
        DistanceMiles = dto.DistanceMiles,
        MatchingCuisineCount = dto.MatchingCuisineCount,
        MatchingBudzCount = dto.MatchingBudzCount,
        HasActiveSlotReservation = dto.HasActiveSlotReservation,
        IsDiscountActive = dto.IsDiscountActive,
        DiscountPercent = dto.DiscountPercent,
    };
}
