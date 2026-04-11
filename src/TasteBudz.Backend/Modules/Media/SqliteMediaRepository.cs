using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Media;

/// <summary>
/// SQLite-backed repository for media assets stored directly in the database.
/// </summary>
public sealed class SqliteMediaRepository(TasteBudzDbContext dbContext) : IMediaRepository
{
    public async Task<MediaAsset?> GetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.MediaAssets.AsNoTracking().FirstOrDefaultAsync(item => item.Id == mediaAssetId, cancellationToken);
        return entity is null ? null : MapMediaAsset(entity);
    }

    public async Task<MediaAsset?> GetProfileAvatarAsync(Guid profileUserId, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.MediaAssets
            .AsNoTracking()
            .Where(item => item.ProfileUserId == profileUserId)
            .ToListAsync(cancellationToken);

        var entity = entities
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault();

        return entity is null ? null : MapMediaAsset(entity);
    }

    public async Task<IReadOnlyCollection<MediaAsset>> ListReportAttachmentsAsync(Guid reportId, CancellationToken cancellationToken = default) =>
        (await dbContext.MediaAssets
            .AsNoTracking()
            .Where(item => item.ReportId == reportId)
            .ToListAsync(cancellationToken))
        .OrderBy(item => item.CreatedAtUtc)
        .Select(MapMediaAsset)
        .ToArray();

    public async Task SaveAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.MediaAssets.FirstOrDefaultAsync(item => item.Id == mediaAsset.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.MediaAssets.Add(ToEntity(mediaAsset));
        }
        else
        {
            entity.OwnerUserId = mediaAsset.OwnerUserId;
            entity.ProfileUserId = mediaAsset.ProfileUserId;
            entity.GroupId = mediaAsset.GroupId;
            entity.EventId = mediaAsset.EventId;
            entity.ReportId = mediaAsset.ReportId;
            entity.OriginalFileName = mediaAsset.OriginalFileName;
            entity.ContentType = mediaAsset.ContentType;
            entity.ContentLength = mediaAsset.ContentLength;
            entity.Content = mediaAsset.Content.ToArray();
            entity.CreatedAtUtc = mediaAsset.CreatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.MediaAssets.FirstOrDefaultAsync(item => item.Id == mediaAssetId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        dbContext.MediaAssets.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MediaAsset MapMediaAsset(MediaAssetEntity entity) =>
        new(
            entity.Id,
            entity.OwnerUserId,
            entity.ProfileUserId,
            entity.GroupId,
            entity.EventId,
            entity.ReportId,
            entity.OriginalFileName,
            entity.ContentType,
            entity.ContentLength,
            entity.Content.ToArray(),
            entity.CreatedAtUtc);

    private static MediaAssetEntity ToEntity(MediaAsset mediaAsset) =>
        new()
        {
            Id = mediaAsset.Id,
            OwnerUserId = mediaAsset.OwnerUserId,
            ProfileUserId = mediaAsset.ProfileUserId,
            GroupId = mediaAsset.GroupId,
            EventId = mediaAsset.EventId,
            ReportId = mediaAsset.ReportId,
            OriginalFileName = mediaAsset.OriginalFileName,
            ContentType = mediaAsset.ContentType,
            ContentLength = mediaAsset.ContentLength,
            Content = mediaAsset.Content.ToArray(),
            CreatedAtUtc = mediaAsset.CreatedAtUtc,
        };
}
