using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class GroupEventHistoryItem
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset EventStartAtUtc { get; init; }
    public int Capacity { get; init; }
    public int ActiveParticipants { get; init; }
    public string? CuisineTarget { get; init; }
    public IReadOnlyList<EventFeedbackItem> Feedback { get; init; } = [];
    public double? AverageRating { get; init; }
    public bool IsCompleted { get; init; }
    public string EventDateLabel => EventStartAtUtc.ToLocalTime().ToString("ddd, MMM d", CultureInfo.InvariantCulture);
    public string EventTimeLabel => EventStartAtUtc.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture);
    public string ParticipationLabel => $"{ActiveParticipants} / {Capacity} joined";
    public string EventAccessLabel => string.Equals(EventType, nameof(TasteBudz.Backend.Domain.EventType.Closed), StringComparison.OrdinalIgnoreCase)
        ? "Private event"
        : "Public event";
    public string EventStatusLabel => Status;
    public bool IsHistory => Status is nameof(EventStatus.Completed) or nameof(EventStatus.Cancelled);
    public bool IsPlanned => !IsHistory;
    public bool IsCancelled => Status is nameof(EventStatus.Cancelled);
    public string? AverageRatingLabel => AverageRating.HasValue
        ? $"{AverageRating.Value.ToString("0.0", CultureInfo.InvariantCulture)} / 5 average"
        : null;

    public static GroupEventHistoryItem FromDto(
        EventSummaryDto dto,
        IReadOnlyCollection<EventFeedbackDto> feedback,
        Guid currentUserId)
    {
        var feedbackItems = feedback
            .Select(item => EventFeedbackItem.FromDto(item, currentUserId))
            .ToList();

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
            Feedback = feedbackItems,
            AverageRating = feedbackItems.Count == 0 ? null : feedbackItems.Average(item => item.Rating),
            IsCompleted = dto.Status == EventStatus.Completed,
        };
    }
}
