// Unit tests for current-user profile updates.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Profiles;


internal sealed class ProfileSlowAvatarMediaRepository : IMediaRepository
{
    public bool IsAvatarLookupInProgress { get; private set; }

    public Task<MediaAsset?> GetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MediaAsset?>(null);

    public async Task<MediaAsset?> GetProfileAvatarAsync(Guid profileUserId, CancellationToken cancellationToken = default)
    {
        IsAvatarLookupInProgress = true;

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);
            return null;
        }
        finally
        {
            IsAvatarLookupInProgress = false;
        }
    }

    public Task<IReadOnlyCollection<MediaAsset>> ListReportAttachmentsAsync(Guid reportId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<MediaAsset>>(Array.Empty<MediaAsset>());

    public Task SaveAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(Guid mediaAssetId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
