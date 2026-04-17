using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

internal static class EventFeedbackMapper
{
    internal static EventFeedbackDto ToDto(
        EventFeedback feedback,
        UserAccount account,
        UserProfile? profile,
        IReadOnlyCollection<EventFeedbackPhotoDto> photos) =>
        new(
            feedback.Id,
            feedback.EventId,
            feedback.AuthorUserId,
            account.Username,
            profile?.DisplayName ?? account.Username,
            feedback.Rating,
            feedback.Text,
            photos,
            feedback.CreatedAtUtc,
            feedback.UpdatedAtUtc);

    internal static EventFeedbackPhotoDto ToPhoto(MediaAsset mediaAsset) =>
        new(
            mediaAsset.Id,
            mediaAsset.OriginalFileName,
            mediaAsset.ContentType,
            mediaAsset.ContentLength,
            mediaAsset.CreatedAtUtc);
}
