using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
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

    public Guid? GroupId { get; set; }
    public string? GroupName { get; init; }
    public bool IsGroupEvent => GroupId.HasValue;

    public IReadOnlyList<RestaurantPickerItem> Restaurants { get; init; } = [];

    public static IReadOnlyList<string> AvailableCuisineTags => CuisineData.AvailableCuisineTags;

    public CreateEventRequest ToRequest() => new()
    {
        EventType = EventType!.Value,
        EventStartAtUtc = new DateTimeOffset(EventStartAt!.Value, TimeSpan.Zero),
        Capacity = Capacity!.Value,
        Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
        CuisineTarget = string.IsNullOrWhiteSpace(CuisineTarget) ? null : CuisineTarget.Trim(),
        SelectedRestaurantId = SelectedRestaurantId,
        GroupId = GroupId,
    };
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
    public string GoogleMapsUrl { get; init; } = string.Empty;

    public static RestaurantPickerItem FromDto(RestaurantDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name,
        Location = $"{dto.City}, {dto.State}",
        PriceTier = new string('$', (int)dto.PriceTier + 1),
        CuisineTags = string.Join(", ", dto.CuisineTags),
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        GoogleMapsUrl = RestaurantMapsLinkBuilder.BuildGoogleMapsUrl(dto),
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
    public Guid? GroupId { get; init; }
    public SelectedRestaurantItem? SelectedRestaurant { get; init; }
    public EventSlotReservationDto? SlotReservation { get; init; }
    public DiscountActivationDto? DiscountActivation { get; init; }
    public IReadOnlyList<RestaurantSlotDto> ReservableSlots { get; init; } = [];
    public IReadOnlyList<EventParticipantItem> Participants { get; init; } = [];
    public IReadOnlyList<EventFeedbackItem> Feedback { get; init; } = [];
    public bool CanSubmitFeedback { get; init; }
    public EventFeedbackFormViewModel FeedbackForm { get; init; } = new();
    public double? AverageRating { get; init; }

    public static EventDetailViewModel FromDto(
        EventDetailDto dto,
        IReadOnlyCollection<EventParticipantDto> participants,
        Guid currentUserId,
        RestaurantDto? selectedRestaurant = null,
        IReadOnlyCollection<RestaurantSlotDto>? reservableSlots = null,
        IReadOnlyCollection<EventFeedbackDto>? feedback = null)
    {
        var feedbackItems = (feedback ?? Array.Empty<EventFeedbackDto>())
            .Select(item => EventFeedbackItem.FromDto(item, currentUserId))
            .ToList();
        var existingFeedback = feedbackItems.FirstOrDefault(item => item.AuthorUserId == currentUserId);
        var isJoinedParticipant = participants.Any(p =>
            p.UserId == currentUserId &&
            p.State == EventParticipantState.Joined);

        return new()
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
            SelectedRestaurant = selectedRestaurant is null ? null : SelectedRestaurantItem.FromDto(selectedRestaurant),
            SlotReservation = dto.SlotReservation,
            DiscountActivation = dto.DiscountActivation,
            ReservableSlots = (reservableSlots ?? Array.Empty<RestaurantSlotDto>())
                .OrderBy(slot => slot.StartsAtUtc)
                .ToList(),
            Participants = participants
                .Where(p => p.State == EventParticipantState.Joined)
                .Select(p => EventParticipantItem.FromDto(p, dto.HostUserId))
                .ToList(),
            IsParticipant = isJoinedParticipant,
            Feedback = feedbackItems,
            CanSubmitFeedback = dto.Status == EventStatus.Completed && isJoinedParticipant,
            FeedbackForm = new EventFeedbackFormViewModel
            {
                EventId = dto.EventId,
                Rating = existingFeedback?.Rating,
                Text = existingFeedback?.Text ?? string.Empty,
            },
            AverageRating = feedbackItems.Count == 0 ? null : feedbackItems.Average(item => item.Rating),
        };
    }
}

public sealed class SelectedRestaurantItem
{
    public Guid RestaurantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string PriceTier { get; init; } = string.Empty;
    public string CuisineTags { get; init; } = string.Empty;
    public string GoogleMapsUrl { get; init; } = string.Empty;

    public static SelectedRestaurantItem FromDto(RestaurantDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name,
        Location = $"{dto.City}, {dto.State} {dto.ZipCode}",
        PriceTier = new string('$', (int)dto.PriceTier + 1),
        CuisineTags = string.Join(", ", dto.CuisineTags),
        GoogleMapsUrl = RestaurantMapsLinkBuilder.BuildGoogleMapsUrl(dto),
    };
}

internal static class RestaurantMapsLinkBuilder
{
    private const string GooglePlaceIdPrefix = "google:";
    private const string OpenStreetMapPlaceIdPrefix = "osm:";

    public static string BuildGoogleMapsUrl(RestaurantDto restaurant)
    {
        var query = $"{restaurant.Name}, {restaurant.City}, {restaurant.State} {restaurant.ZipCode}".Trim();
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://www.google.com/maps/search/?api=1&query={encodedQuery}";

        var externalPlaceId = restaurant.ExternalPlaceId?.Trim();
        if (string.IsNullOrWhiteSpace(externalPlaceId) ||
            externalPlaceId.StartsWith(OpenStreetMapPlaceIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (externalPlaceId.StartsWith(GooglePlaceIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            externalPlaceId = externalPlaceId[GooglePlaceIdPrefix.Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(externalPlaceId) ||
            externalPlaceId.Contains(':', StringComparison.Ordinal))
        {
            return url;
        }

        return $"{url}&query_place_id={Uri.EscapeDataString(externalPlaceId)}";
    }
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

public sealed class EventFeedbackFormViewModel
{
    public Guid EventId { get; set; }

    [Required(ErrorMessage = "Rating is required.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int? Rating { get; set; }

    [Required(ErrorMessage = "Feedback is required.")]
    [MaxLength(1000, ErrorMessage = "Feedback cannot exceed 1000 characters.")]
    public string Text { get; set; } = string.Empty;

    public UpsertEventFeedbackRequest ToRequest() => new()
    {
        Rating = Rating,
        Text = Text,
    };
}

public sealed class EventFeedbackItem
{
    public Guid FeedbackId { get; init; }
    public Guid EventId { get; init; }
    public Guid AuthorUserId { get; init; }
    public string AuthorUsername { get; init; } = string.Empty;
    public string AuthorDisplayName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string Text { get; init; } = string.Empty;
    public IReadOnlyList<EventFeedbackPhotoItem> Photos { get; init; } = [];
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public bool IsCurrentUser { get; init; }

    public static EventFeedbackItem FromDto(EventFeedbackDto dto, Guid currentUserId) => new()
    {
        FeedbackId = dto.FeedbackId,
        EventId = dto.EventId,
        AuthorUserId = dto.AuthorUserId,
        AuthorUsername = dto.AuthorUsername,
        AuthorDisplayName = dto.AuthorDisplayName,
        Rating = dto.Rating,
        Text = dto.Text,
        Photos = dto.Photos.Select(EventFeedbackPhotoItem.FromDto).ToList(),
        CreatedAtUtc = dto.CreatedAtUtc,
        UpdatedAtUtc = dto.UpdatedAtUtc,
        IsCurrentUser = dto.AuthorUserId == currentUserId,
    };
}

public sealed class EventFeedbackPhotoItem
{
    public Guid MediaAssetId { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long ContentLength { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }

    public static EventFeedbackPhotoItem FromDto(EventFeedbackPhotoDto dto) => new()
    {
        MediaAssetId = dto.MediaAssetId,
        OriginalFileName = dto.OriginalFileName,
        ContentType = dto.ContentType,
        ContentLength = dto.ContentLength,
        CreatedAtUtc = dto.CreatedAtUtc,
    };
}
