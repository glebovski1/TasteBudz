// Unit tests for current-user profile updates.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Profiles;


internal sealed class ProfileConcurrentPreferenceGuardProfileRepository(
    ProfileSlowAvatarMediaRepository mediaRepository,
    TestClock clock) : IProfileRepository
{
    private UserProfile? profile;
    private UserPreferences? preferences;

    public Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(profile?.UserId == userId ? profile : null);

    public Task SaveProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        this.profile = profile;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<UserProfile>> ListProfilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<UserProfile>>(profile is null ? Array.Empty<UserProfile>() : new[] { profile });

    public Task<UserPreferences?> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (mediaRepository.IsAvatarLookupInProgress)
        {
            throw new InvalidOperationException("Profile preferences must not be read while avatar lookup is still in progress.");
        }

        return Task.FromResult(preferences?.UserId == userId ? preferences : null);
    }

    public Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        this.preferences = preferences;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RecurringAvailabilityWindow>> ListRecurringAvailabilityAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<RecurringAvailabilityWindow>>(Array.Empty<RecurringAvailabilityWindow>());

    public Task<IReadOnlyCollection<OneOffAvailabilityWindow>> ListOneOffAvailabilityAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<OneOffAvailabilityWindow>>(Array.Empty<OneOffAvailabilityWindow>());

    public Task<RecurringAvailabilityWindow?> GetRecurringAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default) =>
        Task.FromResult<RecurringAvailabilityWindow?>(null);

    public Task<OneOffAvailabilityWindow?> GetOneOffAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default) =>
        Task.FromResult<OneOffAvailabilityWindow?>(null);

    public Task SaveRecurringAvailabilityAsync(RecurringAvailabilityWindow window, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SaveOneOffAvailabilityAsync(OneOffAvailabilityWindow window, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteRecurringAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteOneOffAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<PrivacySettings?> GetPrivacySettingsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<PrivacySettings?>(new PrivacySettings(userId, true, clock.UtcNow));

    public Task SavePrivacySettingsAsync(PrivacySettings settings, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyCollection<UserBlock>> ListBlocksAsync(Guid blockerUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<UserBlock>>(Array.Empty<UserBlock>());

    public Task<UserBlock?> GetBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult<UserBlock?>(null);

    public Task SaveBlockAsync(UserBlock block, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
