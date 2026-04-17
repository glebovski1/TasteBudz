using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// In-memory feedback repository used by unit tests.
/// </summary>
public sealed class InMemoryEventFeedbackRepository(InMemoryTasteBudzStore store) : IEventFeedbackRepository
{
    public Task<EventFeedback?> GetAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.EventFeedbacks.TryGetValue(feedbackId, out var feedback);
            return Task.FromResult(feedback);
        }
    }

    public Task<EventFeedback?> GetForAuthorAsync(Guid eventId, Guid authorUserId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var feedback = store.EventFeedbacks.Values
                .FirstOrDefault(item => item.EventId == eventId && item.AuthorUserId == authorUserId);
            return Task.FromResult(feedback);
        }
    }

    public Task<IReadOnlyCollection<EventFeedback>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.EventFeedbacks.Values
                .Where(item => item.EventId == eventId)
                .OrderByDescending(item => item.UpdatedAtUtc)
                .ThenBy(item => item.Id)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<EventFeedback>>(items);
        }
    }

    public Task SaveAsync(EventFeedback feedback, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.EventFeedbacks[feedback.Id] = feedback;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<EventFeedbackPhoto>> ListPhotosForFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.EventFeedbackPhotos.Values
                .Where(photo => photo.EventFeedbackId == feedbackId)
                .OrderBy(photo => photo.CreatedAtUtc)
                .ThenBy(photo => photo.MediaAssetId)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<EventFeedbackPhoto>>(items);
        }
    }

    public Task<EventFeedbackPhoto?> GetPhotoAsync(Guid feedbackId, Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.EventFeedbackPhotos.TryGetValue(ToPhotoKey(feedbackId, mediaAssetId), out var photo);
            return Task.FromResult(photo);
        }
    }

    public Task<EventFeedbackPhoto?> GetPhotoByMediaAssetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var photo = store.EventFeedbackPhotos.Values
                .FirstOrDefault(item => item.MediaAssetId == mediaAssetId);
            return Task.FromResult(photo);
        }
    }

    public Task SavePhotoAsync(EventFeedbackPhoto photo, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.EventFeedbackPhotos[ToPhotoKey(photo.EventFeedbackId, photo.MediaAssetId)] = photo;
            return Task.CompletedTask;
        }
    }

    public Task DeletePhotoAsync(Guid feedbackId, Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.EventFeedbackPhotos.Remove(ToPhotoKey(feedbackId, mediaAssetId));
            return Task.CompletedTask;
        }
    }

    private static string ToPhotoKey(Guid feedbackId, Guid mediaAssetId) => $"{feedbackId:N}:{mediaAssetId:N}";
}
