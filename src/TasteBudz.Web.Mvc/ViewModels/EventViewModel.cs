using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;

// Browse event page models.



// Create event page models.

public sealed record EventCreateViewModel
{
    public const int RestaurantPickerPageSize = 8;

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
    public Guid? SelectedSlotId { get; set; }
    public int? BrowserTimeZoneOffsetMinutes { get; set; }

    public Guid? GroupId { get; set; }
    public string? GroupName { get; init; }
    public GroupVisibility? GroupVisibility { get; init; }
    public bool IsGroupEvent => GroupId.HasValue;
    public bool HasForcedEventType => IsGroupEvent && GroupVisibility.HasValue;
    public EventType? ForcedEventType => !HasForcedEventType
        ? null
        : GroupVisibility == TasteBudz.Backend.Domain.GroupVisibility.Private
            ? TasteBudz.Backend.Domain.EventType.Closed
            : TasteBudz.Backend.Domain.EventType.Open;
    public string ForcedEventTypeLabel => ForcedEventType switch
    {
        TasteBudz.Backend.Domain.EventType.Closed => "Closed - invite only",
        TasteBudz.Backend.Domain.EventType.Open => "Open - anyone can join",
        _ => "Choose event type",
    };
    public string GroupEventTypeNotice => GroupVisibility == TasteBudz.Backend.Domain.GroupVisibility.Private
        ? "Private group events are closed and invite-only."
        : "Public group events are open for direct joins.";

    public IReadOnlyList<RestaurantPickerItem> Restaurants { get; init; } = [];
    public RestaurantPickerPage RestaurantPage { get; init; } = RestaurantPickerPage.Empty;

    public static IReadOnlyList<string> AvailableCuisineTags => CuisineData.AvailableCuisineTags;

    public CreateEventRequest ToRequest()
    {
        var cuisineTarget = SelectedRestaurantId.HasValue
            ? null
            : string.IsNullOrWhiteSpace(CuisineTarget) ? null : CuisineTarget.Trim();

        return new()
        {
            EventType = EventType!.Value,
            EventStartAtUtc = ConvertLocalInputToUtc(EventStartAt!.Value, BrowserTimeZoneOffsetMinutes),
            Capacity = Capacity!.Value,
            Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
            CuisineTarget = cuisineTarget,
            SelectedRestaurantId = SelectedRestaurantId,
            GroupId = GroupId,
        };
    }

    public static DateTimeOffset ConvertLocalInputToUtc(DateTime localInput, int? browserTimeZoneOffsetMinutes)
    {
        if (browserTimeZoneOffsetMinutes.HasValue)
        {
            var offset = TimeSpan.FromMinutes(-browserTimeZoneOffsetMinutes.Value);
            if (offset >= TimeSpan.FromHours(-14) && offset <= TimeSpan.FromHours(14))
            {
                return new DateTimeOffset(localInput, offset).ToUniversalTime();
            }
        }

        var serverLocal = DateTime.SpecifyKind(localInput, DateTimeKind.Local);
        return new DateTimeOffset(serverLocal).ToUniversalTime();
    }
}




// Detail and feedback page models.
