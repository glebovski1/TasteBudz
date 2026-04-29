// Unit tests for block-side cleanup of live shared contexts.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Profiles;

/// <summary>
/// Verifies that blocking separates users from live shared contexts without erasing history.
/// </summary>
public sealed class BlockingServiceTests
{
    [Fact]
    public async Task CreateAsync_RemovesBudConnectionAndLeavesSharedLiveContexts()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var blocker = await RegisterAsync(services.AuthService, "blocker", "blocker@example.com");
        var blocked = await RegisterAsync(services.AuthService, "blocked", "blocked@example.com");
        var eventId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        await services.DiscoveryRepository.SaveBudConnectionAsync(new BudConnection(
            Guid.NewGuid(),
            blocker.CurrentUser.UserId,
            blocked.CurrentUser.UserId,
            BudConnectionState.Connected,
            clock.UtcNow.AddDays(-2),
            null));
        await SaveEventWithJoinedPairAsync(services.EventRepository, eventId, blocker.CurrentUser.UserId, blocked.CurrentUser.UserId, hostUserId: Guid.NewGuid(), EventStatus.Open, clock);
        await SaveGroupWithActivePairAsync(services.GroupRepository, groupId, blocker.CurrentUser.UserId, blocked.CurrentUser.UserId, ownerUserId: Guid.NewGuid(), clock);

        await services.BlockingService.CreateAsync(blocker.CurrentUser.UserId, new CreateBlockRequest
        {
            BlockedUserId = blocked.CurrentUser.UserId,
        });

        var connection = await services.DiscoveryRepository.GetBudConnectionAsync(blocker.CurrentUser.UserId, blocked.CurrentUser.UserId);
        var blockerParticipant = await services.EventRepository.GetParticipantAsync(eventId, blocker.CurrentUser.UserId);
        var blockedParticipant = await services.EventRepository.GetParticipantAsync(eventId, blocked.CurrentUser.UserId);
        var blockerMembership = await services.GroupRepository.GetMemberAsync(groupId, blocker.CurrentUser.UserId);
        var blockedMembership = await services.GroupRepository.GetMemberAsync(groupId, blocked.CurrentUser.UserId);

        Assert.Equal(BudConnectionState.Removed, connection!.State);
        Assert.Equal(EventParticipantState.Left, blockerParticipant!.State);
        Assert.Equal(EventParticipantState.Joined, blockedParticipant!.State);
        Assert.Equal(GroupMemberState.Left, blockerMembership!.State);
        Assert.Equal(GroupMemberState.Active, blockedMembership!.State);
    }

    [Fact]
    public async Task CreateAsync_WhenBlockerOwnsSharedContexts_RemovesBlockedUserInstead()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var blocker = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var blocked = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var eventId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        await SaveEventWithJoinedPairAsync(services.EventRepository, eventId, blocker.CurrentUser.UserId, blocked.CurrentUser.UserId, blocker.CurrentUser.UserId, EventStatus.Confirmed, clock);
        await SaveGroupWithActivePairAsync(services.GroupRepository, groupId, blocker.CurrentUser.UserId, blocked.CurrentUser.UserId, blocker.CurrentUser.UserId, clock);

        await services.BlockingService.CreateAsync(blocker.CurrentUser.UserId, new CreateBlockRequest
        {
            BlockedUserId = blocked.CurrentUser.UserId,
        });

        var blockerParticipant = await services.EventRepository.GetParticipantAsync(eventId, blocker.CurrentUser.UserId);
        var blockedParticipant = await services.EventRepository.GetParticipantAsync(eventId, blocked.CurrentUser.UserId);
        var blockerMembership = await services.GroupRepository.GetMemberAsync(groupId, blocker.CurrentUser.UserId);
        var blockedMembership = await services.GroupRepository.GetMemberAsync(groupId, blocked.CurrentUser.UserId);

        Assert.Equal(EventParticipantState.Joined, blockerParticipant!.State);
        Assert.Equal(EventParticipantState.Removed, blockedParticipant!.State);
        Assert.Equal(GroupMemberState.Active, blockerMembership!.State);
        Assert.Equal(GroupMemberState.Removed, blockedMembership!.State);
    }

    [Fact]
    public async Task CreateAsync_DoesNotChangeCompletedSharedEvents()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var blocker = await RegisterAsync(services.AuthService, "blocker", "blocker@example.com");
        var blocked = await RegisterAsync(services.AuthService, "blocked", "blocked@example.com");
        var completedEventId = Guid.NewGuid();

        await SaveEventWithJoinedPairAsync(services.EventRepository, completedEventId, blocker.CurrentUser.UserId, blocked.CurrentUser.UserId, hostUserId: Guid.NewGuid(), EventStatus.Completed, clock);

        await services.BlockingService.CreateAsync(blocker.CurrentUser.UserId, new CreateBlockRequest
        {
            BlockedUserId = blocked.CurrentUser.UserId,
        });

        var blockerParticipant = await services.EventRepository.GetParticipantAsync(completedEventId, blocker.CurrentUser.UserId);
        var blockedParticipant = await services.EventRepository.GetParticipantAsync(completedEventId, blocked.CurrentUser.UserId);

        Assert.Equal(EventParticipantState.Joined, blockerParticipant!.State);
        Assert.Equal(EventParticipantState.Joined, blockedParticipant!.State);
    }

    [Fact]
    public async Task RemoveAsync_DoesNotRestoreSeparatedContexts()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var blocker = await RegisterAsync(services.AuthService, "blocker", "blocker@example.com");
        var blocked = await RegisterAsync(services.AuthService, "blocked", "blocked@example.com");
        var eventId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        await services.DiscoveryRepository.SaveBudConnectionAsync(new BudConnection(
            Guid.NewGuid(),
            blocker.CurrentUser.UserId,
            blocked.CurrentUser.UserId,
            BudConnectionState.Connected,
            clock.UtcNow.AddDays(-2),
            null));
        await SaveEventWithJoinedPairAsync(services.EventRepository, eventId, blocker.CurrentUser.UserId, blocked.CurrentUser.UserId, hostUserId: Guid.NewGuid(), EventStatus.Open, clock);
        await SaveGroupWithActivePairAsync(services.GroupRepository, groupId, blocker.CurrentUser.UserId, blocked.CurrentUser.UserId, ownerUserId: Guid.NewGuid(), clock);

        await services.BlockingService.CreateAsync(blocker.CurrentUser.UserId, new CreateBlockRequest
        {
            BlockedUserId = blocked.CurrentUser.UserId,
        });
        await services.BlockingService.RemoveAsync(blocker.CurrentUser.UserId, blocked.CurrentUser.UserId);

        var connection = await services.DiscoveryRepository.GetBudConnectionAsync(blocker.CurrentUser.UserId, blocked.CurrentUser.UserId);
        var blockerParticipant = await services.EventRepository.GetParticipantAsync(eventId, blocker.CurrentUser.UserId);
        var blockerMembership = await services.GroupRepository.GetMemberAsync(groupId, blocker.CurrentUser.UserId);

        Assert.Equal(BudConnectionState.Removed, connection!.State);
        Assert.Equal(EventParticipantState.Left, blockerParticipant!.State);
        Assert.Equal(GroupMemberState.Left, blockerMembership!.State);
    }

    private static async Task<SessionDto> RegisterAsync(AuthService authService, string username, string email) =>
        await authService.RegisterAsync(new RegisterUserRequest
        {
            Username = username,
            Email = email,
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

    private static async Task SaveEventWithJoinedPairAsync(
        IEventRepository eventRepository,
        Guid eventId,
        Guid blockerUserId,
        Guid blockedUserId,
        Guid hostUserId,
        EventStatus status,
        TestClock clock)
    {
        await eventRepository.SaveAsync(new Event(
            eventId,
            hostUserId,
            "Shared dinner",
            EventType.Open,
            status,
            clock.UtcNow.AddDays(status == EventStatus.Completed ? -1 : 1),
            clock.UtcNow.AddHours(status == EventStatus.Completed ? -26 : 2),
            4,
            2,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            null,
            null,
            null,
            clock.UtcNow.AddDays(-5),
            clock.UtcNow,
            null,
            status == EventStatus.Completed ? clock.UtcNow.AddHours(-1) : null));
        await eventRepository.SaveParticipantAsync(new EventParticipant(eventId, blockerUserId, EventParticipantState.Joined, null, clock.UtcNow.AddDays(-4), clock.UtcNow.AddDays(-4), null, null));
        await eventRepository.SaveParticipantAsync(new EventParticipant(eventId, blockedUserId, EventParticipantState.Joined, null, clock.UtcNow.AddDays(-4), clock.UtcNow.AddDays(-4), null, null));
    }

    private static async Task SaveGroupWithActivePairAsync(
        IGroupRepository groupRepository,
        Guid groupId,
        Guid blockerUserId,
        Guid blockedUserId,
        Guid ownerUserId,
        TestClock clock)
    {
        await groupRepository.SaveAsync(new Group(
            groupId,
            ownerUserId,
            "Shared group",
            null,
            GroupVisibility.Public,
            GroupWallpaperTheme.Default,
            GroupLifecycleState.Active,
            clock.UtcNow.AddDays(-5),
            clock.UtcNow));
        await groupRepository.SaveMemberAsync(new GroupMember(groupId, blockerUserId, GroupMemberState.Active, clock.UtcNow.AddDays(-5), clock.UtcNow));
        await groupRepository.SaveMemberAsync(new GroupMember(groupId, blockedUserId, GroupMemberState.Active, clock.UtcNow.AddDays(-5), clock.UtcNow));
    }

    private static TestServices CreateServices(TestClock clock)
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var discoveryRepository = new InMemoryDiscoveryRepository(store);
        var eventRepository = new InMemoryEventRepository(store);
        var groupRepository = new InMemoryGroupRepository(store);
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var blockingService = new BlockingService(authRepository, profileRepository, discoveryRepository, eventRepository, groupRepository, clock);

        return new TestServices(authService, blockingService, discoveryRepository, eventRepository, groupRepository);
    }

    private sealed record TestServices(
        AuthService AuthService,
        BlockingService BlockingService,
        IDiscoveryRepository DiscoveryRepository,
        IEventRepository EventRepository,
        IGroupRepository GroupRepository);
}
