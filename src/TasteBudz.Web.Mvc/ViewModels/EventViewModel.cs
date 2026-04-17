using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;

// ── Browse ───────────────────────────────────────────────────────────────────

public sealed class EventIndexViewModel
{
    public IReadOnlyList<EventSummaryItem> Events { get; init; } = [];
    public string? SearchQuery { get; init; }

    public static EventIndexViewModel Empty => new();

    public static EventIndexViewModel FromDto(
        IEnumerable<EventSummaryDto> events,
        string? searchQuery = null) => new()
        {
            Events = events.Select(EventSummaryItem.FromDto).ToList(),
            SearchQuery = searchQuery,
        };
}

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
    };
}

// ── Create ───────────────────────────────────────────────────────────────────

public sealed record EventCreateViewModel
{
    [MaxLength(120, ErrorMessage = "Title cannot exceed 120 characters.")]
    [Display(Name = "Event Title")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Please select Open or Closed.")]
    [Display(Name = "Event Type")]
    public EventType? EventType { get; set; }

    [Required(ErrorMessage = "Date and time is required.")]
    [Display(Name = "Date & Time")]
    public DateTime? EventStartAt { get; set; }

    [Required(ErrorMessage = "Capacity is required.")]
    [Range(2, 8, ErrorMessage = "Capacity must be between 2 and 8.")]
    [Display(Name = "Capacity")]
    public int? Capacity { get; set; }

    [MaxLength(120, ErrorMessage = "Cuisine target cannot exceed 120 characters.")]
    [Display(Name = "Cuisine / Food Preference")]
    public string? CuisineTarget { get; set; }

    [Display(Name = "Restaurant")]
    public Guid? SelectedRestaurantId { get; set; }
    public string? SelectedRestaurantName { get; set; }

    public IReadOnlyList<RestaurantPickerItem> Restaurants { get; init; } = [];

    public static IReadOnlyList<string> AvailableCuisineTags => CuisineData.AvailableCuisineTags;

    public CreateEventRequest ToRequest()
    {
        // The DB constraint requires exactly one of SelectedRestaurantId or CuisineTarget.
        // If the user picked a restaurant, clear CuisineTarget so both are never sent together.
        // If neither is set, send CuisineTarget as null and let EventPolicy give a clean error.
        var cuisineTarget = SelectedRestaurantId.HasValue
            ? null
            : string.IsNullOrWhiteSpace(CuisineTarget) ? null : CuisineTarget.Trim();

        return new()
        {
            EventType = EventType!.Value,
            EventStartAtUtc = new DateTimeOffset(EventStartAt!.Value, TimeSpan.Zero),
            Capacity = Capacity!.Value,
            Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
            CuisineTarget = cuisineTarget,
            SelectedRestaurantId = SelectedRestaurantId,
        };
    }
}

public sealed class RestaurantPickerItem
{
    public Guid RestaurantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string PriceTier { get; init; } = string.Empty;
    public string CuisineTags { get; init; } = string.Empty;
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }

    public static RestaurantPickerItem FromDto(RestaurantDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name,
        Location = $"{dto.City}, {dto.State}",
        PriceTier = new string('$', (int)dto.PriceTier + 1),
        CuisineTags = string.Join(", ", dto.CuisineTags),
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
    };
}

// ── Detail ───────────────────────────────────────────────────────────────────

public sealed class EventDetailViewModel
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset EventStartAtUtc { get; init; }
    public int Capacity { get; init; }
    public int ActiveParticipants { get; init; }
    public string? CuisineTarget { get; init; }
    public bool IsHost { get; init; }
    public bool IsParticipant { get; init; }
    public bool IsInvited { get; init; }
    public Guid? GroupId { get; init; }
    public IReadOnlyList<EventParticipantItem> Participants { get; init; } = [];

    // Populated only for the host of a closed event — used to render the invite panel.
    public IReadOnlyList<BudConnectionDto> Budz { get; init; } = [];
    public IReadOnlyList<InvitableGroup> InvitableGroups { get; init; } = [];

    public static EventDetailViewModel FromDto(
        EventDetailDto dto,
        IReadOnlyCollection<EventParticipantDto> participants,
        Guid currentUserId,
        IReadOnlyList<BudConnectionDto>? budz = null,
        IReadOnlyList<InvitableGroup>? invitableGroups = null) => new()
        {
            EventId = dto.EventId,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "Untitled Event" : dto.Title,
            EventType = dto.EventType.ToString(),
            Status = dto.Status.ToString(),
            EventStartAtUtc = dto.EventStartAtUtc,
            Capacity = dto.Capacity,
            ActiveParticipants = dto.ActiveParticipants,
            CuisineTarget = dto.CuisineTarget,
            IsHost = dto.HostUserId == currentUserId,
            GroupId = dto.GroupId,
            Participants = participants
                .Where(p => p.State == EventParticipantState.Joined)
                .Select(p => EventParticipantItem.FromDto(p, dto.HostUserId))
                .ToList(),
            IsParticipant = participants.Any(p =>
                p.UserId == currentUserId &&
                p.State == EventParticipantState.Joined),
            IsInvited = participants.Any(p =>
                p.UserId == currentUserId &&
                p.State == EventParticipantState.Invited),
            Budz = budz ?? [],
            InvitableGroups = invitableGroups ?? [],
        };
}

/// <summary>A group the host belongs to, with its active members for invite selection.</summary>
public sealed class InvitableGroup
{
    public Guid GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<GroupMemberDto> Members { get; init; } = [];
}

public sealed class EventParticipantItem
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public bool IsHost { get; init; }

    public static EventParticipantItem FromDto(EventParticipantDto dto, Guid hostUserId) => new()
    {
        UserId = dto.UserId,
        DisplayName = dto.DisplayName,
        Username = dto.Username,
        IsHost = dto.UserId == hostUserId,
    };
}