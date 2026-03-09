// Unit tests for the event workflow rules that are easiest to validate in isolation.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Events;

/// <summary>
/// Covers host auto-join behavior and the invite-accept capacity rule.
/// </summary>
public sealed class EventWorkflowTests
{
    /// <summary>
    /// Service callers must hit the same capacity guard that controller callers do.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public async Task CreateAsync_WithOutOfRangeCapacity_RejectsRequest(int capacity)
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var hostSession = await RegisterAsync(services.AuthService, "host", "host@example.com");
        var host = ToCurrentUser(hostSession);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.EventService.CreateAsync(host, new CreateEventRequest
            {
                Title = "Invalid capacity",
                EventType = EventType.Open,
                EventStartAtUtc = clock.UtcNow.AddDays(2),
                Capacity = capacity,
                SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            }));

        Assert.Equal(400, exception.StatusCode);
    }

    /// <summary>
    /// Creating an event must immediately create the host's joined participant row.
    /// </summary>
    [Fact]
    public async Task CreateAsync_AutoJoinsHostAndCountsTowardCapacity()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var hostSession = await RegisterAsync(services.AuthService, "host", "host@example.com");
        var host = ToCurrentUser(hostSession);

        var detail = await services.EventService.CreateAsync(host, new CreateEventRequest
        {
            Title = "Friday Sushi Night",
            EventType = EventType.Open,
            EventStartAtUtc = clock.UtcNow.AddDays(2),
            Capacity = 4,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });

        var participants = await services.EventRepository.ListParticipantsAsync(detail.EventId);
        var participant = Assert.Single(participants);

        Assert.Equal(1, detail.ActiveParticipants);
        Assert.Equal(EventStatus.Open, detail.Status);
        Assert.Equal(host.UserId, participant.UserId);
        Assert.Equal(EventParticipantState.Joined, participant.State);
    }

    /// <summary>
    /// Closed-event invites remain seatless until acceptance, so the final acceptance can still fail on capacity.
    /// </summary>
    [Fact]
    public async Task UpdateMyParticipationAsync_ClosedInviteAcceptFailsWhenEventIsAlreadyFull()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var hostSession = await RegisterAsync(services.AuthService, "host", "host@example.com");
        var bobSession = await RegisterAsync(services.AuthService, "bob", "bob@example.com");
        var samSession = await RegisterAsync(services.AuthService, "sam", "sam@example.com");
        var host = ToCurrentUser(hostSession);
        var bob = ToCurrentUser(bobSession);
        var sam = ToCurrentUser(samSession);

        var detail = await services.EventService.CreateAsync(host, new CreateEventRequest
        {
            Title = "Invite Only Curry",
            EventType = EventType.Closed,
            EventStartAtUtc = clock.UtcNow.AddDays(2),
            Capacity = 2,
            CuisineTarget = "Indian",
            InviteUsernames = new[] { "bob", "sam" },
        });

        await services.EventParticipationService.UpdateMyParticipationAsync(bob, detail.EventId, new UpdateMyParticipationRequest
        {
            State = EventParticipantState.Joined,
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.EventParticipationService.UpdateMyParticipationAsync(sam, detail.EventId, new UpdateMyParticipationRequest
            {
                State = EventParticipantState.Joined,
            }));
        var participants = await services.EventRepository.ListParticipantsAsync(detail.EventId);

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(2, participants.Count(participant => participant.State == EventParticipantState.Joined));
        Assert.Contains(participants, participant => participant.UserId == host.UserId && participant.State == EventParticipantState.Joined);
        Assert.Contains(participants, participant => participant.UserId == bob.UserId && participant.State == EventParticipantState.Joined);
        Assert.Contains(participants, participant => participant.UserId == sam.UserId && participant.State == EventParticipantState.Invited);
    }

    /// <summary>
    /// Update flows must enforce the same hard size bounds as create flows.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public async Task UpdateAsync_WithOutOfRangeCapacity_RejectsRequest(int capacity)
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var hostSession = await RegisterAsync(services.AuthService, "host", "host@example.com");
        var host = ToCurrentUser(hostSession);

        var detail = await services.EventService.CreateAsync(host, new CreateEventRequest
        {
            Title = "Friday Sushi Night",
            EventType = EventType.Open,
            EventStartAtUtc = clock.UtcNow.AddDays(2),
            Capacity = 4,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.EventService.UpdateAsync(host.UserId, detail.EventId, new UpdateEventRequest
            {
                Capacity = capacity,
            }));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WithNonMemberGroupLink_ReturnsForbidden()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var hostSession = await RegisterAsync(services.AuthService, "host", "host@example.com");
        var ownerSession = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var host = ToCurrentUser(hostSession);
        var owner = ToCurrentUser(ownerSession);
        var groupId = Guid.NewGuid();

        services.Store.Groups[groupId] = new Group(groupId, owner.UserId, "Private crew", null, GroupVisibility.Private, GroupLifecycleState.Active, clock.UtcNow, clock.UtcNow);
        services.Store.GroupMembers[$"{groupId:N}:{owner.UserId:N}"] = new GroupMember(groupId, owner.UserId, GroupMemberState.Active, clock.UtcNow, clock.UtcNow);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.EventService.CreateAsync(host, new CreateEventRequest
            {
                Title = "Unauthorized link",
                EventType = EventType.Open,
                EventStartAtUtc = clock.UtcNow.AddDays(2),
                Capacity = 4,
                SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                GroupId = groupId,
            }));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WithActiveGroupMemberLink_Succeeds()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var hostSession = await RegisterAsync(services.AuthService, "host", "host@example.com");
        var ownerSession = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var host = ToCurrentUser(hostSession);
        var owner = ToCurrentUser(ownerSession);
        var groupId = Guid.NewGuid();

        services.Store.Groups[groupId] = new Group(groupId, owner.UserId, "Dinner club", null, GroupVisibility.Private, GroupLifecycleState.Active, clock.UtcNow, clock.UtcNow);
        services.Store.GroupMembers[$"{groupId:N}:{owner.UserId:N}"] = new GroupMember(groupId, owner.UserId, GroupMemberState.Active, clock.UtcNow, clock.UtcNow);
        services.Store.GroupMembers[$"{groupId:N}:{host.UserId:N}"] = new GroupMember(groupId, host.UserId, GroupMemberState.Active, clock.UtcNow, clock.UtcNow);

        var detail = await services.EventService.CreateAsync(host, new CreateEventRequest
        {
            Title = "Authorized link",
            EventType = EventType.Open,
            EventStartAtUtc = clock.UtcNow.AddDays(2),
            Capacity = 4,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            GroupId = groupId,
        });

        Assert.Equal(groupId, detail.GroupId);
    }

    /// <summary>
    /// Registers a deterministic user so event tests can focus on workflow rules instead of auth setup.
    /// </summary>
    private static async Task<SessionDto> RegisterAsync(AuthService authService, string username, string email) =>
        await authService.RegisterAsync(new RegisterUserRequest
        {
            Username = username,
            Email = email,
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

    /// <summary>
    /// Converts the auth response into the current-user shape expected by the service layer.
    /// </summary>
    private static CurrentUser ToCurrentUser(SessionDto session) =>
        new(session.CurrentUser.UserId, session.CurrentUser.Username, session.CurrentUser.Roles);

    /// <summary>
    /// Builds the in-memory service graph used across the event workflow tests.
    /// </summary>
    private static TestServices CreateServices(TestClock clock)
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var restaurantRepository = new InMemoryRestaurantRepository(store);
        var eventRepository = new InMemoryEventRepository(store);
        var groupRepository = new InMemoryGroupRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var keyedLockProvider = new InMemoryKeyedLockProvider();
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var inviteService = new EventInviteService(eventRepository, authRepository, profileRepository, notificationService, lifecycleService, keyedLockProvider, clock);
        var eventService = new EventService(eventRepository, restaurantRepository, groupRepository, authRepository, profileRepository, notificationService, lifecycleService, inviteService, keyedLockProvider, clock);
        var participationService = new EventParticipationService(eventRepository, authRepository, profileRepository, notificationService, lifecycleService, keyedLockProvider, clock);

        return new TestServices(store, authService, eventService, participationService, eventRepository);
    }

    /// <summary>
    /// Small bundle that keeps the unit-test setup readable.
    /// </summary>
    private sealed record TestServices(
        InMemoryTasteBudzStore Store,
        AuthService AuthService,
        EventService EventService,
        EventParticipationService EventParticipationService,
        IEventRepository EventRepository);
}
