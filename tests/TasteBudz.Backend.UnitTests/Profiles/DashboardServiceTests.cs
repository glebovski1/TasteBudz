// Unit tests for dashboard profile composition.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Profiles;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_DoesNotOverlapAvatarAndPreferenceReads()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero));
        var store = new InMemoryTasteBudzStore();
        var authRepository = new InMemoryAuthRepository(store);
        var mediaRepository = new SlowAvatarMediaRepository();
        var profileRepository = new ConcurrentPreferenceGuardProfileRepository(mediaRepository, clock);
        var eventRepository = new InMemoryEventRepository(store);
        var groupRepository = new InMemoryGroupRepository(store);
        var moderationRepository = new InMemoryModerationRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var discoveryRepository = new InMemoryDiscoveryRepository(store);
        var userId = Guid.NewGuid();
        var account = new UserAccount(userId, "alex", "ALEX", "alex@example.com", "ALEX@EXAMPLE.COM", "hash", AccountStatus.Active, new[] { UserRole.User }, clock.UtcNow, clock.UtcNow, null);
        await authRepository.CreateAccountAsync(account);
        await profileRepository.SaveProfileAsync(new UserProfile(userId, "Alex", null, "45220", SocialGoal.Friends, clock.UtcNow, clock.UtcNow));
        await profileRepository.SavePreferencesAsync(new UserPreferences(userId, new[] { "Italian" }, SpiceTolerance.Medium, Array.Empty<string>(), Array.Empty<string>(), clock.UtcNow));
        var service = new DashboardService(
            authRepository,
            profileRepository,
            mediaRepository,
            new UserEventQueryService(eventRepository, new EventLifecycleService(eventRepository, notificationService, clock)),
            new UserGroupQueryService(groupRepository),
            new DiscoveryService(
                authRepository,
                profileRepository,
                discoveryRepository,
                new RestrictionService(moderationRepository, authRepository, new AuditLogService(moderationRepository), clock),
                notificationService,
                clock));

        var dashboard = await service.GetDashboardAsync(userId);

        Assert.Equal("alex", dashboard.Profile.Username);
        Assert.Equal(new[] { "Italian" }, dashboard.Profile.CuisineTags);
    }

    private sealed class SlowAvatarMediaRepository : IMediaRepository
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

    private sealed class ConcurrentPreferenceGuardProfileRepository(
        SlowAvatarMediaRepository mediaRepository,
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
}
