using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Media;

/// <summary>
/// In-memory media repository used by service-level tests.
/// </summary>
public sealed class InMemoryMediaRepository(InMemoryTasteBudzStore store) : IMediaRepository
{
    public Task<MediaAsset?> GetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.MediaAssets.TryGetValue(mediaAssetId, out var mediaAsset);
            return Task.FromResult(mediaAsset);
        }
    }

    public Task<MediaAsset?> GetProfileAvatarAsync(Guid profileUserId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var avatar = store.MediaAssets.Values
                .Where(asset => asset.ProfileUserId == profileUserId)
                .OrderByDescending(asset => asset.CreatedAtUtc)
                .FirstOrDefault();

            return Task.FromResult(avatar);
        }
    }

    public Task<IReadOnlyCollection<MediaAsset>> ListReportAttachmentsAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<MediaAsset>>(
                store.MediaAssets.Values
                    .Where(asset => asset.ReportId == reportId)
                    .OrderBy(asset => asset.CreatedAtUtc)
                    .ToArray());
        }
    }

    public Task SaveAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.MediaAssets[mediaAsset.Id] = mediaAsset;
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.MediaAssets.Remove(mediaAssetId);
            return Task.CompletedTask;
        }
    }
}
