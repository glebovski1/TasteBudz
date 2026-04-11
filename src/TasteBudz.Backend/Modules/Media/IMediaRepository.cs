using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Media;

/// <summary>
/// Persistence boundary for database-backed media assets.
/// </summary>
public interface IMediaRepository
{
    Task<MediaAsset?> GetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);

    Task<MediaAsset?> GetProfileAvatarAsync(Guid profileUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MediaAsset>> ListReportAttachmentsAsync(Guid reportId, CancellationToken cancellationToken = default);

    Task SaveAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);
}
