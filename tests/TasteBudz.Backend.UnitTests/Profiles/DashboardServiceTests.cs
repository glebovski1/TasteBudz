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
        var mediaRepository = new DashboardSlowAvatarMediaRepository();
        var profileRepository = new DashboardConcurrentPreferenceGuardProfileRepository(mediaRepository, clock);
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
            new UserEventQueryService(eventRepository, profileRepository, new EventLifecycleService(eventRepository, notificationService, clock)),
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


}
