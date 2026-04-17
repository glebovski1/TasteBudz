namespace TasteBudz.Backend.Modules.Events;

public sealed record EventFeedbackDto(
    Guid FeedbackId,
    Guid EventId,
    Guid AuthorUserId,
    string AuthorUsername,
    string AuthorDisplayName,
    int Rating,
    string Text,
    IReadOnlyCollection<EventFeedbackPhotoDto> Photos,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
