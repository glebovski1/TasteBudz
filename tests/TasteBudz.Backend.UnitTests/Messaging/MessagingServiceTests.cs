// Unit tests for scoped messaging access and restriction rules.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Media;
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

    [Fact]
    public async Task SendAsync_DirectChatBetweenBudzPersistsMessage()
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
        var match = await services.DiscoveryService.RecordSwipeAsync(ToCurrentUser(sam), new RecordSwipeDecisionRequest
        {
            SubjectUserId = alex.CurrentUser.UserId,
            Decision = SwipeDecisionType.Like,
        });
        var directChat = await services.MessagingService.CreateDirectChatAsync(ToCurrentUser(alex), new CreateDirectChatRequest
        {
            SubjectUserId = sam.CurrentUser.UserId,
        });

        var sent = await services.MessagingService.SendDirectMessageAsync(ToCurrentUser(alex), directChat.DirectChatId, new SendDirectChatMessageRequest
        {
            Body = "Want to grab ramen?",
        });
        var history = await services.MessagingService.ListDirectMessagesAsync(sam.CurrentUser.UserId, directChat.DirectChatId, new ChatHistoryQuery());

        Assert.Equal(match.BudConnectionId, directChat.DirectChatId);
        Assert.Equal("Want to grab ramen?", sent.Body);
        Assert.Single(history.Items);
    }

    [Fact]
    public async Task CreateDirectChatAsync_WhenUsersAreNotBudz_ReturnsNotFound()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var alex = await RegisterAsync(services.AuthService, "alex", "alex@example.com");
        var sam = await RegisterAsync(services.AuthService, "sam", "sam@example.com");

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.MessagingService.CreateDirectChatAsync(ToCurrentUser(alex), new CreateDirectChatRequest
            {
                SubjectUserId = sam.CurrentUser.UserId,
            }));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task SendDirectMessageAsync_WhenEitherUserBlocked_ReturnsNotFound()
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
        await services.DiscoveryService.RecordSwipeAsync(ToCurrentUser(sam), new RecordSwipeDecisionRequest
        {
            SubjectUserId = alex.CurrentUser.UserId,
            Decision = SwipeDecisionType.Like,
        });
        var directChat = await services.MessagingService.CreateDirectChatAsync(ToCurrentUser(alex), new CreateDirectChatRequest
        {
            SubjectUserId = sam.CurrentUser.UserId,
        });
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(alex.CurrentUser.UserId, sam.CurrentUser.UserId, clock.UtcNow));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.MessagingService.SendDirectMessageAsync(ToCurrentUser(alex), directChat.DirectChatId, new SendDirectChatMessageRequest
            {
                Body = "This should be hidden",
            }));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task ListEventMessagesAsync_WhenCompletedEventUsersAreBlocked_HidesBlockedPairMessages()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var alex = await RegisterAsync(services.AuthService, "alex", "alex@example.com");
        var sam = await RegisterAsync(services.AuthService, "sam", "sam@example.com");
        var host = await RegisterAsync(services.AuthService, "host", "host@example.com");
        var eventId = Guid.NewGuid();
        var threadId = Guid.NewGuid();

        await services.EventRepository.SaveAsync(new Event(
            eventId,
            host.CurrentUser.UserId,
            "Past dinner",
            EventType.Open,
            EventStatus.Completed,
            clock.UtcNow.AddDays(-1),
            clock.UtcNow.AddDays(-2),
            4,
            2,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            null,
            null,
            null,
            clock.UtcNow.AddDays(-5),
            clock.UtcNow,
            null,
            clock.UtcNow.AddHours(-1)));
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(eventId, alex.CurrentUser.UserId, EventParticipantState.Joined, null, clock.UtcNow.AddDays(-3), clock.UtcNow.AddDays(-3), null, null));
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(eventId, sam.CurrentUser.UserId, EventParticipantState.Joined, null, clock.UtcNow.AddDays(-3), clock.UtcNow.AddDays(-3), null, null));
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(eventId, host.CurrentUser.UserId, EventParticipantState.Joined, null, clock.UtcNow.AddDays(-3), clock.UtcNow.AddDays(-3), null, null));
        await services.MessagingRepository.SaveThreadAsync(new ChatThread(threadId, ChatScopeType.Event, eventId, clock.UtcNow.AddDays(-3)));
        await services.MessagingRepository.SaveMessageAsync(new ChatMessage(Guid.NewGuid(), threadId, sam.CurrentUser.UserId, "hidden from alex", clock.UtcNow.AddHours(-3)));
        await services.MessagingRepository.SaveMessageAsync(new ChatMessage(Guid.NewGuid(), threadId, host.CurrentUser.UserId, "still visible", clock.UtcNow.AddHours(-2)));
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(alex.CurrentUser.UserId, sam.CurrentUser.UserId, clock.UtcNow));

        var history = await services.MessagingService.ListEventMessagesAsync(alex.CurrentUser.UserId, eventId, new ChatHistoryQuery());

        var item = Assert.Single(history.Items);
        Assert.Equal("still visible", item.Body);
    }

    [Fact]
    public async Task SupportChat_UserCanMessageAndAdminCanReply()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var user = await RegisterAsync(services.AuthService, "alex", "alex@example.com");
        var admin = await RegisterAsync(services.AuthService, "admin", "admin@example.com");
        var adminUser = new CurrentUser(admin.CurrentUser.UserId, admin.CurrentUser.Username, new[] { UserRole.Admin });

        var userMessage = await services.MessagingService.SendMySupportMessageAsync(ToCurrentUser(user), new SendSupportMessageRequest
        {
            Body = "I need help with my account",
        });
        var threads = await services.MessagingService.ListSupportThreadsAsync(adminUser);
        var adminMessage = await services.MessagingService.SendSupportMessageForUserAsync(adminUser, user.CurrentUser.UserId, new SendSupportMessageRequest
        {
            Body = "We can help with that",
        });
        var history = await services.MessagingService.ListSupportMessagesForUserAsync(adminUser, user.CurrentUser.UserId, new ChatHistoryQuery());

        Assert.Equal("I need help with my account", userMessage.Body);
        var thread = Assert.Single(threads);
        Assert.Equal(user.CurrentUser.UserId, thread.UserId);
        Assert.Equal("We can help with that", adminMessage.Body);
        Assert.Equal(2, history.Items.Count);
    }

    [Fact]
    public async Task ListSupportMessagesForUserAsync_WhenUserIsNotAdmin_ReturnsNotFound()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var alex = await RegisterAsync(services.AuthService, "alex", "alex@example.com");
        var sam = await RegisterAsync(services.AuthService, "sam", "sam@example.com");

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.MessagingService.ListSupportMessagesForUserAsync(ToCurrentUser(sam), alex.CurrentUser.UserId, new ChatHistoryQuery()));

        Assert.Equal(404, exception.StatusCode);
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
        var discoveryRepository = new InMemoryDiscoveryRepository(store);
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
        var mediaRepository = new InMemoryMediaRepository(store);
        var groupService = new GroupService(groupRepository, eventRepository, authRepository, profileRepository, mediaRepository, notificationService, lifecycleService, clock);
        var discoveryService = new DiscoveryService(authRepository, profileRepository, discoveryRepository, restrictionService, notificationService, clock, keyedLockProvider: new InMemoryKeyedLockProvider());
        var messagingService = new MessagingService(messagingRepository, eventRepository, groupRepository, discoveryRepository, authRepository, profileRepository, new AlwaysOnFeatureFlagService(), restrictionService, clock);

        return new TestServices(authService, profileRepository, restrictionService, eventService, participationService, groupService, discoveryService, messagingService, eventRepository, messagingRepository);
    }

    private sealed record TestServices(
        AuthService AuthService,
        IProfileRepository ProfileRepository,
        RestrictionService RestrictionService,
        EventService EventService,
        EventParticipationService ParticipationService,
        GroupService GroupService,
        DiscoveryService DiscoveryService,
        MessagingService MessagingService,
        IEventRepository EventRepository,
        IMessagingRepository MessagingRepository);

}
