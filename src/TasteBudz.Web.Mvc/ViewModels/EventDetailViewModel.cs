using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class EventDetailViewModel
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string EventAccessLabel => string.Equals(EventType, nameof(TasteBudz.Backend.Domain.EventType.Closed), StringComparison.OrdinalIgnoreCase)
        ? "Private event"
        : "Public event";
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset EventStartAtUtc { get; init; }
    public int Capacity { get; init; }
    public int ActiveParticipants { get; init; }
    public string? CuisineTarget { get; init; }
    public bool IsHost { get; init; }
    public bool IsParticipant { get; init; }
    public bool IsInvited { get; init; }
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
    public IReadOnlyList<BudConnectionDto> Budz { get; init; } = [];
    public IReadOnlyList<InvitableGroup> InvitableGroups { get; init; } = [];
    public ChatViewModel? EventChat { get; init; }

    public static EventDetailViewModel FromDto(
        EventDetailDto dto,
        IReadOnlyCollection<EventParticipantDto> participants,
        Guid currentUserId,
        RestaurantDto? selectedRestaurant = null,
        IReadOnlyCollection<RestaurantSlotDto>? reservableSlots = null,
        IReadOnlyCollection<EventFeedbackDto>? feedback = null,
        IReadOnlyList<BudConnectionDto>? budz = null,
        IReadOnlyList<InvitableGroup>? invitableGroups = null,
        ChatViewModel? eventChat = null)
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
            IsInvited = participants.Any(p =>
                p.UserId == currentUserId &&
                p.State == EventParticipantState.Invited),
            Feedback = feedbackItems,
            CanSubmitFeedback = dto.Status == EventStatus.Completed && isJoinedParticipant,
            FeedbackForm = new EventFeedbackFormViewModel
            {
                EventId = dto.EventId,
                Rating = existingFeedback?.Rating,
                Text = existingFeedback?.Text ?? string.Empty,
            },
            AverageRating = feedbackItems.Count == 0 ? null : feedbackItems.Average(item => item.Rating),
            Budz = budz ?? [],
            InvitableGroups = invitableGroups ?? [],
            EventChat = eventChat,
        };
    }
}
