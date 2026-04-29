using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


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
