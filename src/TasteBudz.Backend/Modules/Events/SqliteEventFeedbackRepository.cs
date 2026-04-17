using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// SQLite-backed repository for event feedback entries and photo links.
/// </summary>
public sealed class SqliteEventFeedbackRepository(TasteBudzDbContext dbContext) : IEventFeedbackRepository
{
    public async Task<EventFeedback?> GetAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventFeedbacks.AsNoTracking().FirstOrDefaultAsync(item => item.Id == feedbackId, cancellationToken);
        return entity is null ? null : MapFeedback(entity);
    }

    public async Task<EventFeedback?> GetForAuthorAsync(Guid eventId, Guid authorUserId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventFeedbacks
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.EventId == eventId && item.AuthorUserId == authorUserId, cancellationToken);
        return entity is null ? null : MapFeedback(entity);
    }

    public async Task<IReadOnlyCollection<EventFeedback>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        (await dbContext.EventFeedbacks
            .AsNoTracking()
            .Where(item => item.EventId == eventId)
            .ToListAsync(cancellationToken))
        .Select(MapFeedback)
        .OrderByDescending(item => item.UpdatedAtUtc)
        .ThenBy(item => item.Id)
        .ToArray();

    public async Task SaveAsync(EventFeedback feedback, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventFeedbacks.FirstOrDefaultAsync(item => item.Id == feedback.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.EventFeedbacks.Add(ToEntity(feedback));
        }
        else
        {
            entity.EventId = feedback.EventId;
            entity.AuthorUserId = feedback.AuthorUserId;
            entity.Rating = feedback.Rating;
            entity.Text = feedback.Text;
            entity.CreatedAtUtc = feedback.CreatedAtUtc;
            entity.UpdatedAtUtc = feedback.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EventFeedbackPhoto>> ListPhotosForFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken = default) =>
        (await dbContext.EventFeedbackPhotos
            .AsNoTracking()
            .Where(item => item.EventFeedbackId == feedbackId)
            .ToListAsync(cancellationToken))
        .Select(MapPhoto)
        .OrderBy(item => item.CreatedAtUtc)
        .ThenBy(item => item.MediaAssetId)
        .ToArray();

    public async Task<EventFeedbackPhoto?> GetPhotoAsync(Guid feedbackId, Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventFeedbackPhotos
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.EventFeedbackId == feedbackId && item.MediaAssetId == mediaAssetId, cancellationToken);
        return entity is null ? null : MapPhoto(entity);
    }

    public async Task<EventFeedbackPhoto?> GetPhotoByMediaAssetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventFeedbackPhotos
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.MediaAssetId == mediaAssetId, cancellationToken);
        return entity is null ? null : MapPhoto(entity);
    }

    public async Task SavePhotoAsync(EventFeedbackPhoto photo, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventFeedbackPhotos.FirstOrDefaultAsync(
            item => item.EventFeedbackId == photo.EventFeedbackId && item.MediaAssetId == photo.MediaAssetId,
            cancellationToken);

        if (entity is null)
        {
            dbContext.EventFeedbackPhotos.Add(ToEntity(photo));
        }
        else
        {
            entity.CreatedAtUtc = photo.CreatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePhotoAsync(Guid feedbackId, Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventFeedbackPhotos.FirstOrDefaultAsync(
            item => item.EventFeedbackId == feedbackId && item.MediaAssetId == mediaAssetId,
            cancellationToken);

        if (entity is null)
        {
            return;
        }

        dbContext.EventFeedbackPhotos.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static EventFeedback MapFeedback(EventFeedbackEntity entity) =>
        new(
            entity.Id,
            entity.EventId,
            entity.AuthorUserId,
            entity.Rating,
            entity.Text,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

    private static EventFeedbackPhoto MapPhoto(EventFeedbackPhotoEntity entity) =>
        new(entity.EventFeedbackId, entity.MediaAssetId, entity.CreatedAtUtc);

    private static EventFeedbackEntity ToEntity(EventFeedback feedback) =>
        new()
        {
            Id = feedback.Id,
            EventId = feedback.EventId,
            AuthorUserId = feedback.AuthorUserId,
            Rating = feedback.Rating,
            Text = feedback.Text,
            CreatedAtUtc = feedback.CreatedAtUtc,
            UpdatedAtUtc = feedback.UpdatedAtUtc,
        };

    private static EventFeedbackPhotoEntity ToEntity(EventFeedbackPhoto photo) =>
        new()
        {
            EventFeedbackId = photo.EventFeedbackId,
            MediaAssetId = photo.MediaAssetId,
            CreatedAtUtc = photo.CreatedAtUtc,
        };
}
