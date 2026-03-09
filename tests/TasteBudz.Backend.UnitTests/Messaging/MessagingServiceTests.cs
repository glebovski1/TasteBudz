// Unit tests for scoped messaging access and restriction rules.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Messaging;

/// <summary>
/// Verifies scope-derived chat access and chat-send restriction enforcement.
/// </summary>
public sealed class MessagingServiceTests
{
    [Fact]
    public async Task SendAsync_EventParticipantCanSendAndReadHistory()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = await RegisterAsync(services.AuthService, "host", "host@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var eventDetail = await services.EventService.CreateAsync(ToCurrentUser(host), new CreateEventRequest
        {
            Title = "Chat event",
            EventType = EventType.Open,
            EventStartAtUtc = clock.UtcNow.AddDays(1),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });

        await services.ParticipationService.JoinOpenEventAsync(ToCurrentUser(guest), eventDetail.EventId);
        var sent = await services.MessagingService.SendAsync(ToCurrentUser(guest), new SendChatMessageRequest
        {
            ScopeType = ChatScopeType.Event,
            ScopeId = eventDetail.EventId,
            Body = "See you there",
        });
        var history = await services.MessagingService.ListEventMessagesAsync(guest.CurrentUser.UserId, eventDetail.EventId, new ChatHistoryQuery());

        Assert.Equal("See you there", sent.Body);
        Assert.Single(history.Items);
    }

    [Fact]
    public async Task SendAsync_WhenChatSendRestricted_ReturnsForbidden()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var moderator = await RegisterAsync(services.AuthService, "mod", "mod@example.com");
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Chat Group",
            Visibility = GroupVisibility.Public,
        });
        await services.GroupService.JoinAsync(guest.CurrentUser.UserId, group.GroupId);
        await services.RestrictionService.CreateAsync(
            new CurrentUser(moderator.CurrentUser.UserId, moderator.CurrentUser.Username, new[] { UserRole.Moderator }),
            new CreateRestrictionRequest
            {
                SubjectUserId = guest.CurrentUser.UserId,
                Scope = RestrictionScope.ChatSend,
                Reason = "Cooldown",
            });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.MessagingService.SendAsync(ToCurrentUser(guest), new SendChatMessageRequest
            {
                ScopeType = ChatScopeType.Group,
                ScopeId = group.GroupId,
                Body = "I should be blocked",
            }));

        Assert.Equal(403, exception.StatusCode);
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
        var restaurantRepository = new InMemoryRestaurantRepository(store);
        var eventRepository = new InMemoryEventRepository(store);
        var groupRepository = new InMemoryGroupRepository(store);
        var messagingRepository = new InMemoryMessagingRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var moderationRepository = new InMemoryModerationRepository(store);
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var auditLogService = new AuditLogService(moderationRepository);
        var restrictionService = new RestrictionService(moderationRepository, authRepository, auditLogService, clock);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var inviteService = new EventInviteService(eventRepository, authRepository, profileRepository, notificationService, lifecycleService, new InMemoryKeyedLockProvider(), clock);
        var eventService = new EventService(eventRepository, restaurantRepository, groupRepository, authRepository, profileRepository, notificationService, restrictionService, lifecycleService, inviteService, new InMemoryKeyedLockProvider(), clock);
        var participationService = new EventParticipationService(eventRepository, authRepository, profileRepository, notificationService, restrictionService, lifecycleService, new InMemoryKeyedLockProvider(), clock);
        var groupService = new GroupService(groupRepository, eventRepository, authRepository, profileRepository, notificationService, lifecycleService, clock);
        var messagingService = new MessagingService(messagingRepository, eventRepository, groupRepository, authRepository, profileRepository, restrictionService, clock);

        return new TestServices(authService, restrictionService, eventService, participationService, groupService, messagingService);
    }

    private sealed record TestServices(
        AuthService AuthService,
        RestrictionService RestrictionService,
        EventService EventService,
        EventParticipationService ParticipationService,
        GroupService GroupService,
        MessagingService MessagingService);
}
