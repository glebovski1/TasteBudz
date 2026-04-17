using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Persistence boundary for event feedback entries and their photo links.
/// </summary>
public interface IEventFeedbackRepository
{
    Task<EventFeedback?> GetAsync(Guid feedbackId, CancellationToken cancellationToken = default);

    Task<EventFeedback?> GetForAuthorAsync(Guid eventId, Guid authorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EventFeedback>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task SaveAsync(EventFeedback feedback, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EventFeedbackPhoto>> ListPhotosForFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken = default);

    Task<EventFeedbackPhoto?> GetPhotoAsync(Guid feedbackId, Guid mediaAssetId, CancellationToken cancellationToken = default);

    Task<EventFeedbackPhoto?> GetPhotoByMediaAssetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);

    Task SavePhotoAsync(EventFeedbackPhoto photo, CancellationToken cancellationToken = default);

    Task DeletePhotoAsync(Guid feedbackId, Guid mediaAssetId, CancellationToken cancellationToken = default);
}
