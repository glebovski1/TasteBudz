// Unit tests for discovery filtering and Budz creation rules.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Discovery;

/// <summary>
/// Verifies the accepted discovery visibility and mutual-like rules.
/// </summary>
public sealed class DiscoveryServiceTests
{
    [Fact]
    public async Task SearchAsync_FiltersPrivacyBlocksAndDiscoveryRestrictions()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var caller = await RegisterAsync(services.AuthService, "caller", "caller@example.com");
        var visible = await RegisterAsync(services.AuthService, "visible", "visible@example.com");
        var hidden = await RegisterAsync(services.AuthService, "hidden", "hidden@example.com");
        var blocked = await RegisterAsync(services.AuthService, "blocked", "blocked@example.com");
        var restricted = await RegisterAsync(services.AuthService, "restricted", "restricted@example.com");

        await services.ProfileRepository.SavePrivacySettingsAsync(new PrivacySettings(hidden.CurrentUser.UserId, false, clock.UtcNow));
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(caller.CurrentUser.UserId, blocked.CurrentUser.UserId, clock.UtcNow));
        await services.RestrictionService.CreateAsync(
            new CurrentUser(caller.CurrentUser.UserId, caller.CurrentUser.Username, new[] { UserRole.Moderator }),
            new CreateRestrictionRequest
            {
                SubjectUserId = restricted.CurrentUser.UserId,
                Scope = RestrictionScope.DiscoveryVisibility,
                Reason = "Hidden from discovery",
            });

        var result = await services.DiscoveryService.SearchAsync(caller.CurrentUser.UserId, new SearchPeopleQuery { PageSize = 10 });

        Assert.Single(result.Items);
        Assert.Equal(visible.CurrentUser.UserId, result.Items.Single().UserId);
    }

    [Fact]
    public async Task RecordSwipeAsync_ReciprocalLikeCreatesBudAndNotifications()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var alex = await RegisterAsync(services.AuthService, "alex", "alex@example.com");
        var sam = await RegisterAsync(services.AuthService, "sam", "sam@example.com");

        await services.DiscoveryService.RecordSwipeAsync(ToCurrentUser(alex), new RecordSwipeDecisionRequest
        {
            SubjectUserId = sam.CurrentUser.UserId,
            Decision = SwipeDecisionType.Like,
        });

        var result = await services.DiscoveryService.RecordSwipeAsync(ToCurrentUser(sam), new RecordSwipeDecisionRequest
        {
            SubjectUserId = alex.CurrentUser.UserId,
            Decision = SwipeDecisionType.Like,
        });

        Assert.True(result.IsBudMatch);
        Assert.Single(await services.DiscoveryRepository.ListBudConnectionsAsync());
        Assert.Single(await services.NotificationService.ListForUserAsync(alex.CurrentUser.UserId));
        Assert.Single(await services.NotificationService.ListForUserAsync(sam.CurrentUser.UserId));
    }

    private static async Task<SessionDto> RegisterAsync(AuthService authService, string username, string email) =>
        await authService.RegisterAsync(new RegisterUserRequest
        {
            Username = username,
            Email = email,
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

    private static CurrentUser ToCurrentUser(SessionDto session) =>
        new(session.CurrentUser.UserId, session.CurrentUser.Username, session.CurrentUser.Roles);

    private static TestServices CreateServices(TestClock clock)
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var discoveryRepository = new InMemoryDiscoveryRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var moderationRepository = new InMemoryModerationRepository(store);
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var auditLogService = new AuditLogService(moderationRepository);
        var restrictionService = new RestrictionService(moderationRepository, authRepository, auditLogService, clock);
        var discoveryService = new DiscoveryService(authRepository, profileRepository, discoveryRepository, restrictionService, notificationService, clock);

        return new TestServices(authService, discoveryService, profileRepository, discoveryRepository, notificationService, restrictionService);
    }

    private sealed record TestServices(
        AuthService AuthService,
        DiscoveryService DiscoveryService,
        IProfileRepository ProfileRepository,
        IDiscoveryRepository DiscoveryRepository,
        INotificationService NotificationService,
        RestrictionService RestrictionService);
}
