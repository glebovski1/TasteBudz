// Unit tests for the event workflow rules that are easiest to validate in isolation.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Moderation;
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
    public async Task CreateAsync_WithNonOwnerGroupLink_ReturnsForbidden()
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

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.EventService.CreateAsync(host, new CreateEventRequest
            {
                Title = "Unauthorized owner link",
                EventType = EventType.Open,
                EventStartAtUtc = clock.UtcNow.AddDays(2),
                Capacity = 4,
                SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                GroupId = groupId,
            }));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WithOwnerGroupLink_Succeeds()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var ownerSession = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var owner = ToCurrentUser(ownerSession);
        var groupId = Guid.NewGuid();

        services.Store.Groups[groupId] = new Group(groupId, owner.UserId, "Dinner club", null, GroupVisibility.Private, GroupLifecycleState.Active, clock.UtcNow, clock.UtcNow);
        services.Store.GroupMembers[$"{groupId:N}:{owner.UserId:N}"] = new GroupMember(groupId, owner.UserId, GroupMemberState.Active, clock.UtcNow, clock.UtcNow);

        var detail = await services.EventService.CreateAsync(owner, new CreateEventRequest
        {
            Title = "Owner linked event",
            EventType = EventType.Open,
            EventStartAtUtc = clock.UtcNow.AddDays(2),
            Capacity = 4,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            GroupId = groupId,
        });

        Assert.Equal(groupId, detail.GroupId);
    }

    [Fact]
    public async Task JoinOpenEventAsync_WhenRestricted_ReturnsForbidden()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var hostSession = await RegisterAsync(services.AuthService, "host", "host@example.com");
        var guestSession = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var host = ToCurrentUser(hostSession);
        var guest = ToCurrentUser(guestSession);

        var detail = await services.EventService.CreateAsync(host, new CreateEventRequest
        {
            Title = "Restricted join",
            EventType = EventType.Open,
            EventStartAtUtc = clock.UtcNow.AddDays(1),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });

        await services.RestrictionService.CreateAsync(
            new CurrentUser(host.UserId, host.Username, new[] { UserRole.Moderator }),
            new CreateRestrictionRequest
            {
                SubjectUserId = guest.UserId,
                Scope = RestrictionScope.EventJoin,
                Reason = "Too many no-shows",
            });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.EventParticipationService.JoinOpenEventAsync(guest, detail.EventId));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task JoinOpenEventAsync_LastSeatContentionAllowsOnlyOneWinner()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = ToCurrentUser(await RegisterAsync(services.AuthService, "host", "host@example.com"));
        var guestOne = ToCurrentUser(await RegisterAsync(services.AuthService, "guest1", "guest1@example.com"));
        var guestTwo = ToCurrentUser(await RegisterAsync(services.AuthService, "guest2", "guest2@example.com"));

        var detail = await services.EventService.CreateAsync(host, new CreateEventRequest
        {
            Title = "One seat left",
            EventType = EventType.Open,
            EventStartAtUtc = clock.UtcNow.AddDays(1),
            Capacity = 2,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });

        var joinTasks = new[]
        {
            TryJoinAsync(services.EventParticipationService, guestOne, detail.EventId),
            TryJoinAsync(services.EventParticipationService, guestTwo, detail.EventId),
        };

        await Task.WhenAll(joinTasks);

        Assert.Equal(1, joinTasks.Count(task => task.Result.Succeeded));
        Assert.Equal(1, joinTasks.Count(task => !task.Result.Succeeded && task.Result.StatusCode == 409));

        var participants = await services.EventRepository.ListParticipantsAsync(detail.EventId);
        Assert.Equal(2, participants.Count(participant => participant.State == EventParticipantState.Joined));
    }

    [Fact]
    public async Task RemoveParticipantAsync_ModeratorCanRemoveAfterDecisionAt()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = ToCurrentUser(await RegisterAsync(services.AuthService, "host", "host@example.com"));
        var guest = ToCurrentUser(await RegisterAsync(services.AuthService, "guest", "guest@example.com"));
        var moderator = ToCurrentUser(await RegisterAsync(services.AuthService, "mod", "mod@example.com", roles: new[] { UserRole.Moderator }));

        var detail = await services.EventService.CreateAsync(host, new CreateEventRequest
        {
            Title = "Moderator removal",
            EventType = EventType.Open,
            EventStartAtUtc = clock.UtcNow.AddHours(1),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });

        await services.EventParticipationService.JoinOpenEventAsync(guest, detail.EventId);
        clock.Advance(TimeSpan.FromMinutes(50));

        await services.EventParticipationService.RemoveParticipantAsync(moderator, detail.EventId, guest.UserId);

        var participant = await services.EventRepository.GetParticipantAsync(detail.EventId, guest.UserId);
        Assert.Equal(EventParticipantState.Removed, participant!.State);
    }

    [Fact]
    public async Task CreateAsync_WithBlockedInvitee_ReturnsForbidden()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 18, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = ToCurrentUser(await RegisterAsync(services.AuthService, "host", "host@example.com"));
        var guest = ToCurrentUser(await RegisterAsync(services.AuthService, "guest", "guest@example.com"));
        services.Store.Blocks[$"{host.UserId:N}:{guest.UserId:N}"] = new UserBlock(host.UserId, guest.UserId, clock.UtcNow);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.EventService.CreateAsync(host, new CreateEventRequest
            {
                Title = "Blocked invite",
                EventType = EventType.Closed,
                EventStartAtUtc = clock.UtcNow.AddDays(1),
                Capacity = 4,
                CuisineTarget = "Sushi",
                InviteUsernames = new[] { guest.Username },
            }));

        Assert.Equal(403, exception.StatusCode);
    }

    /// <summary>
    /// Converts the auth response into the current-user shape expected by the service layer.
    /// </summary>
    private static CurrentUser ToCurrentUser(SessionDto session) =>
        new(session.CurrentUser.UserId, session.CurrentUser.Username, session.CurrentUser.Roles);

    private static async Task<(bool Succeeded, int? StatusCode)> TryJoinAsync(
        EventParticipationService service,
        CurrentUser currentUser,
        Guid eventId)
    {
        try
        {
            await service.JoinOpenEventAsync(currentUser, eventId);
            return (true, null);
        }
        catch (ApiException exception)
        {
            return (false, exception.StatusCode);
        }
    }

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
        var moderationRepository = new InMemoryModerationRepository(store);
        var keyedLockProvider = new InMemoryKeyedLockProvider();
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var auditLogService = new AuditLogService(moderationRepository);
        var restrictionService = new RestrictionService(moderationRepository, authRepository, auditLogService, clock);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var inviteService = new EventInviteService(eventRepository, authRepository, profileRepository, notificationService, lifecycleService, keyedLockProvider, clock);
        var eventService = new EventService(eventRepository, restaurantRepository, groupRepository, authRepository, profileRepository, notificationService, restrictionService, lifecycleService, inviteService, keyedLockProvider, clock);
        var participationService = new EventParticipationService(eventRepository, authRepository, profileRepository, notificationService, restrictionService, lifecycleService, keyedLockProvider, clock);

        return new TestServices(store, authService, eventService, participationService, eventRepository, restrictionService);
    }

    /// <summary>
    /// Small bundle that keeps the unit-test setup readable.
    /// </summary>
    private sealed record TestServices(
        InMemoryTasteBudzStore Store,
        AuthService AuthService,
        EventService EventService,
        EventParticipationService EventParticipationService,
        IEventRepository EventRepository,
        RestrictionService RestrictionService);

    /// <summary>
    /// Registers a deterministic user so event tests can focus on workflow rules instead of auth setup.
    /// </summary>
    private static async Task<SessionDto> RegisterAsync(AuthService authService, string username, string email, IReadOnlyCollection<UserRole>? roles = null)
    {
        var session = await authService.RegisterAsync(new RegisterUserRequest
        {
            Username = username,
            Email = email,
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

        if (roles is null)
        {
            return session;
        }

        var updated = session.CurrentUser with { Roles = roles };
        return session with { CurrentUser = updated };
    }
}
