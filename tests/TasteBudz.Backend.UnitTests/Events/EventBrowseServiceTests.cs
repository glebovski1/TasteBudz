// Unit tests for event browse filtering and visibility behavior.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Events;

/// <summary>
/// Verifies the main open-event browse filters without the HTTP layer in the way.
/// </summary>
public sealed class EventBrowseServiceTests
{
    [Fact]
    public async Task BrowseAsync_ReturnsVisibleClosedEventsForCurrentUserWhenRequested()
    {
        var services = CreateServices();
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "45220", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, hostUserId, "45220", "host", services.Clock);

        await SaveEventAsync(
            services.EventRepository,
            new Event(
                eventId,
                hostUserId,
                "Closed dinner",
                EventType.Closed,
                EventStatus.Open,
                services.Clock.UtcNow.AddDays(2),
                services.Clock.UtcNow.AddDays(1),
                4,
                2,
                null,
                "Sushi",
                null,
                null,
                services.Clock.UtcNow,
                services.Clock.UtcNow,
                null,
                null),
            hostUserId,
            services.Clock);
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(
            eventId,
            currentUserId,
            EventParticipantState.Invited,
            services.Clock.UtcNow,
            null,
            null,
            null,
            null));

        var result = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            EventType = EventType.Closed,
            PageSize = 10,
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("Closed dinner", item.Title);
        Assert.Equal(EventType.Closed, item.EventType);
    }

    [Fact]
    public async Task BrowseAsync_IncludesSlotAndDiscountCardIndicators()
    {
        var services = CreateServices(restaurantOperationsEnabled: true, discountsEnabled: true);
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var now = services.Clock.UtcNow;

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "45220", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, hostUserId, "45220", "host", services.Clock);

        await SaveEventAsync(
            services.EventRepository,
            new Event(
                eventId,
                hostUserId,
                "Discounted slot dinner",
                EventType.Open,
                EventStatus.Open,
                now.AddHours(2),
                now.AddHours(1),
                4,
                2,
                restaurantId,
                null,
                null,
                null,
                now,
                now,
                null,
                null),
            hostUserId,
            services.Clock);
        var slot = new RestaurantSlot(
            Guid.NewGuid(),
            restaurantId,
            now.AddHours(1),
            now.AddHours(3),
            4,
            now.AddMinutes(30),
            2,
            30,
            RestaurantSlotStatus.Open,
            now,
            now,
            null,
            null);
        var reservation = new EventSlotReservation(Guid.NewGuid(), eventId, slot.Id, EventSlotReservationStatus.Active, now, null, null);
        await services.RestaurantOperationsRepository.SaveSlotAsync(slot);
        await services.RestaurantOperationsRepository.SaveReservationAsync(reservation);
        await services.RestaurantOperationsRepository.SaveDiscountActivationAsync(new DiscountActivation(reservation.Id, true, false, now));

        var result = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery { PageSize = 10 });

        var item = Assert.Single(result.Items);
        Assert.True(item.HasActiveSlotReservation);
        Assert.True(item.IsDiscountActive);
        Assert.Equal(30, item.DiscountPercent);
    }

    [Fact]
    public async Task BrowseAsync_AppliesGroupStatusAndDateRangeFiltersWithDeterministicOrdering()
    {
        var services = CreateServices();
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var targetGroupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "45220", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, hostUserId, "45220", "host", services.Clock);

        var matchingEarly = new Event(
            Guid.NewGuid(),
            hostUserId,
            "Matching early",
            EventType.Open,
            EventStatus.Open,
            services.Clock.UtcNow.AddHours(3),
            services.Clock.UtcNow.AddHours(2),
            4,
            2,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            null,
            targetGroupId,
            null,
            services.Clock.UtcNow,
            services.Clock.UtcNow,
            null,
            null);
        var matchingLate = matchingEarly with
        {
            Id = Guid.NewGuid(),
            Title = "Matching late",
            EventStartAtUtc = services.Clock.UtcNow.AddHours(4),
            DecisionAtUtc = services.Clock.UtcNow.AddHours(3),
        };
        var wrongGroup = matchingEarly with
        {
            Id = Guid.NewGuid(),
            Title = "Wrong group",
            GroupId = otherGroupId,
        };
        var cancelled = matchingEarly with
        {
            Id = Guid.NewGuid(),
            Title = "Cancelled",
            Status = EventStatus.Cancelled,
            CancellationReason = "Called off",
            CancelledAtUtc = services.Clock.UtcNow,
        };
        var outsideRange = matchingEarly with
        {
            Id = Guid.NewGuid(),
            Title = "Outside range",
            EventStartAtUtc = services.Clock.UtcNow.AddHours(6),
            DecisionAtUtc = services.Clock.UtcNow.AddHours(5),
        };

        foreach (var eventRecord in new[] { outsideRange, matchingLate, wrongGroup, matchingEarly, cancelled })
        {
            await SaveEventAsync(services.EventRepository, eventRecord, hostUserId, services.Clock);
        }

        var result = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            GroupId = targetGroupId,
            Status = EventStatus.Open,
            StartsAfter = services.Clock.UtcNow.AddHours(2),
            StartsBefore = services.Clock.UtcNow.AddHours(5),
            PageSize = 10,
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "Matching early", "Matching late" }, result.Items.Select(item => item.Title));
    }

    [Fact]
    public async Task BrowseAsync_FiltersGroupLinkedAndOrdinaryEvents()
    {
        var services = CreateServices();
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "45220", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, hostUserId, "45220", "host", services.Clock);
        await SaveEventAsync(services.EventRepository, CreateOpenEvent(hostUserId, "Group event", services.Clock.UtcNow.AddDays(1), groupId: groupId), hostUserId, services.Clock);
        await SaveEventAsync(services.EventRepository, CreateOpenEvent(hostUserId, "Ordinary event", services.Clock.UtcNow.AddDays(1).AddHours(1)), hostUserId, services.Clock);

        var groupEvents = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            GroupLinked = true,
            PageSize = 10,
        });
        var ordinaryEvents = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            GroupLinked = false,
            PageSize = 10,
        });

        Assert.Equal(new[] { "Group event" }, groupEvents.Items.Select(item => item.Title));
        Assert.Equal(new[] { "Ordinary event" }, ordinaryEvents.Items.Select(item => item.Title));
    }

    [Fact]
    public async Task BrowseAsync_HidesOpenEventsWithBlockedHostOrParticipant()
    {
        var services = CreateServices();
        var currentUserId = Guid.NewGuid();
        var visibleHostUserId = Guid.NewGuid();
        var blockedHostUserId = Guid.NewGuid();
        var participantHostUserId = Guid.NewGuid();
        var blockedParticipantUserId = Guid.NewGuid();

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "45220", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, visibleHostUserId, "45220", "visible-host", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, blockedHostUserId, "45220", "blocked-host", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, participantHostUserId, "45220", "participant-host", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, blockedParticipantUserId, "45220", "blocked-participant", services.Clock);
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(currentUserId, blockedHostUserId, services.Clock.UtcNow));
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(blockedParticipantUserId, currentUserId, services.Clock.UtcNow));

        await SaveEventAsync(services.EventRepository, CreateOpenEvent(visibleHostUserId, "Visible dinner", services.Clock.UtcNow.AddDays(2)), visibleHostUserId, services.Clock);
        await SaveEventAsync(services.EventRepository, CreateOpenEvent(blockedHostUserId, "Blocked host dinner", services.Clock.UtcNow.AddDays(2).AddHours(2)), blockedHostUserId, services.Clock);
        var blockedParticipantEvent = CreateOpenEvent(participantHostUserId, "Blocked participant dinner", services.Clock.UtcNow.AddDays(2).AddHours(3));
        await SaveEventAsync(services.EventRepository, blockedParticipantEvent, participantHostUserId, services.Clock);
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(
            blockedParticipantEvent.Id,
            blockedParticipantUserId,
            EventParticipantState.Joined,
            null,
            services.Clock.UtcNow,
            services.Clock.UtcNow,
            null,
            null));

        var result = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            PageSize = 10,
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("Visible dinner", item.Title);
    }

    [Fact]
    public async Task BrowseAsync_AvailabilityOnlyMatchesRecurringAndOneOffWindows()
    {
        var services = CreateServices();
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "45220", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, hostUserId, "45220", "host", services.Clock);

        await services.ProfileRepository.SaveRecurringAvailabilityAsync(new RecurringAvailabilityWindow(
            Guid.NewGuid(),
            currentUserId,
            DayOfWeek.Friday,
            new TimeOnly(18, 0),
            new TimeOnly(20, 0),
            "Friday dinner",
            services.Clock.UtcNow,
            services.Clock.UtcNow));
        await services.ProfileRepository.SaveOneOffAvailabilityAsync(new OneOffAvailabilityWindow(
            Guid.NewGuid(),
            currentUserId,
            new DateTimeOffset(2026, 3, 14, 17, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 14, 20, 0, 0, TimeSpan.Zero),
            "Saturday outing",
            services.Clock.UtcNow,
            services.Clock.UtcNow));

        var recurringMatch = CreateOpenEvent(hostUserId, "Recurring match", new DateTimeOffset(2026, 3, 13, 18, 30, 0, TimeSpan.Zero));
        var oneOffMatch = CreateOpenEvent(hostUserId, "One-off match", new DateTimeOffset(2026, 3, 14, 18, 0, 0, TimeSpan.Zero));
        var outsideAvailability = CreateOpenEvent(hostUserId, "Outside availability", new DateTimeOffset(2026, 3, 14, 21, 0, 0, TimeSpan.Zero));

        foreach (var eventRecord in new[] { recurringMatch, oneOffMatch, outsideAvailability })
        {
            await SaveEventAsync(services.EventRepository, eventRecord, hostUserId, services.Clock);
        }

        var result = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            AvailabilityOnly = true,
            PageSize = 10,
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "Recurring match", "One-off match" }, result.Items.Select(item => item.Title));
    }

    [Fact]
    public async Task BrowseAsync_DistanceFilterUsesSelectedRestaurantCoordinates()
    {
        var services = CreateServices();
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "45220", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, hostUserId, "45220", "host", services.Clock);

        var nearbyRestaurantEvent = CreateOpenEvent(
            hostUserId,
            "Nearby sushi",
            services.Clock.UtcNow.AddDays(2),
            selectedRestaurantId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var farRestaurantEvent = CreateOpenEvent(
            hostUserId,
            "Far grill",
            services.Clock.UtcNow.AddDays(2).AddHours(1),
            selectedRestaurantId: Guid.Parse("55555555-5555-5555-5555-555555555555"));

        await SaveEventAsync(services.EventRepository, nearbyRestaurantEvent, hostUserId, services.Clock);
        await SaveEventAsync(services.EventRepository, farRestaurantEvent, hostUserId, services.Clock);

        var result = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            ZipCode = "45220",
            RadiusMiles = 1,
            PageSize = 10,
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("Nearby sushi", item.Title);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task BrowseAsync_DistanceFilterFallsBackToHostZipForCuisineTargetedEvents()
    {
        var services = CreateServices();
        var currentUserId = Guid.NewGuid();
        var nearbyHostUserId = Guid.NewGuid();
        var farHostUserId = Guid.NewGuid();

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "41011", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, nearbyHostUserId, "41011", "nearby-host", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, farHostUserId, "45220", "far-host", services.Clock);

        var nearbyCuisineEvent = CreateOpenEvent(
            nearbyHostUserId,
            "Nearby cuisine target",
            services.Clock.UtcNow.AddDays(3),
            cuisineTarget: "American");
        var farCuisineEvent = CreateOpenEvent(
            farHostUserId,
            "Far cuisine target",
            services.Clock.UtcNow.AddDays(3).AddHours(1),
            cuisineTarget: "Sushi");

        await SaveEventAsync(services.EventRepository, nearbyCuisineEvent, nearbyHostUserId, services.Clock);
        await SaveEventAsync(services.EventRepository, farCuisineEvent, farHostUserId, services.Clock);

        var result = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            ZipCode = "41011",
            RadiusMiles = 1,
            PageSize = 10,
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("Nearby cuisine target", item.Title);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task BrowseAsync_RecommendedModeRanksByDistancePreferencesAndBudz()
    {
        var services = CreateServices();
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var buddyUserId = Guid.NewGuid();

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "45220", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, hostUserId, "45220", "host", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, buddyUserId, "45220", "buddy", services.Clock);
        await services.ProfileRepository.SavePreferencesAsync(new UserPreferences(
            currentUserId,
            new[] { "Sushi" },
            SpiceTolerance.Medium,
            Array.Empty<string>(),
            Array.Empty<string>(),
            services.Clock.UtcNow));
        await services.DiscoveryRepository.SaveBudConnectionAsync(new BudConnection(
            Guid.NewGuid(),
            currentUserId,
            buddyUserId,
            BudConnectionState.Connected,
            services.Clock.UtcNow,
            null));

        var bestMatch = CreateOpenEvent(
            hostUserId,
            "Best match",
            services.Clock.UtcNow.AddDays(2),
            selectedRestaurantId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var nearbyOnly = CreateOpenEvent(
            hostUserId,
            "Nearby only",
            services.Clock.UtcNow.AddDays(2).AddHours(1),
            selectedRestaurantId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var farOnly = CreateOpenEvent(
            hostUserId,
            "Far only",
            services.Clock.UtcNow.AddDays(2).AddHours(2),
            selectedRestaurantId: Guid.Parse("55555555-5555-5555-5555-555555555555"));

        await SaveEventAsync(services.EventRepository, bestMatch, hostUserId, services.Clock);
        await SaveEventAsync(services.EventRepository, nearbyOnly, hostUserId, services.Clock);
        await SaveEventAsync(services.EventRepository, farOnly, hostUserId, services.Clock);
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(
            bestMatch.Id,
            buddyUserId,
            EventParticipantState.Joined,
            null,
            services.Clock.UtcNow,
            services.Clock.UtcNow,
            null,
            null));

        var result = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            Recommended = true,
            PageSize = 10,
        });
        var items = result.Items.ToArray();

        Assert.Equal(new[] { "Best match", "Nearby only", "Far only" }, items.Select(item => item.Title));
        Assert.Equal(1, items[0].MatchingBudzCount);
        Assert.True(items[0].MatchingCuisineCount > 0);
        Assert.True(items[0].DistanceMiles.HasValue);
        Assert.Equal(0, items[2].MatchingCuisineCount);
    }

    [Fact]
    public async Task BrowseAsync_RecommendedModeOnlyReturnsOpenEventsWithAvailableSeats()
    {
        var services = CreateServices();
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var closedInviteId = Guid.NewGuid();

        await SaveProfileAsync(services.ProfileRepository, currentUserId, "45220", "caller", services.Clock);
        await SaveProfileAsync(services.ProfileRepository, hostUserId, "45220", "host", services.Clock);

        var openAvailable = CreateOpenEvent(hostUserId, "Open seat", services.Clock.UtcNow.AddDays(2));
        var full = openAvailable with { Id = Guid.NewGuid(), Title = "Full table", Status = EventStatus.Full, Capacity = 1 };
        var completed = openAvailable with { Id = Guid.NewGuid(), Title = "Completed table", Status = EventStatus.Completed, CompletedAtUtc = services.Clock.UtcNow };
        var cancelled = openAvailable with { Id = Guid.NewGuid(), Title = "Cancelled table", Status = EventStatus.Cancelled, CancellationReason = "Called off", CancelledAtUtc = services.Clock.UtcNow };
        var closedInvite = openAvailable with { Id = closedInviteId, Title = "Closed invite", EventType = EventType.Closed };

        foreach (var eventRecord in new[] { openAvailable, full, completed, cancelled, closedInvite })
        {
            await SaveEventAsync(services.EventRepository, eventRecord, hostUserId, services.Clock);
        }

        await services.EventRepository.SaveParticipantAsync(new EventParticipant(
            closedInviteId,
            currentUserId,
            EventParticipantState.Invited,
            services.Clock.UtcNow,
            null,
            null,
            null,
            null));

        var result = await services.BrowseService.BrowseAsync(currentUserId, new BrowseEventsQuery
        {
            Recommended = true,
            PageSize = 10,
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("Open seat", item.Title);
        Assert.Equal(EventStatus.Open, item.Status);
        Assert.True(item.ActiveParticipants < item.Capacity);
    }

    private static Event CreateOpenEvent(
        Guid hostUserId,
        string title,
        DateTimeOffset startAtUtc,
        Guid? selectedRestaurantId = null,
        string? cuisineTarget = null,
        Guid? groupId = null) =>
        new(
            Guid.NewGuid(),
            hostUserId,
            title,
            EventType.Open,
            EventStatus.Open,
            startAtUtc,
            startAtUtc.AddMinutes(-15),
            4,
            2,
            selectedRestaurantId,
            cuisineTarget,
            groupId,
            null,
            startAtUtc.AddDays(-1),
            startAtUtc.AddDays(-1),
            null,
            null);

    private static async Task SaveEventAsync(IEventRepository repository, Event eventRecord, Guid hostUserId, TestClock clock)
    {
        await repository.SaveAsync(eventRecord);
        await repository.SaveParticipantAsync(new EventParticipant(
            eventRecord.Id,
            hostUserId,
            EventParticipantState.Joined,
            null,
            clock.UtcNow,
            clock.UtcNow,
            null,
            null));
    }

    private static Task SaveProfileAsync(IProfileRepository repository, Guid userId, string zipCode, string displayName, TestClock clock) =>
        repository.SaveProfileAsync(new UserProfile(userId, displayName, null, zipCode, SocialGoal.Friends, clock.UtcNow, clock.UtcNow));

    private static TestServices CreateServices(bool restaurantOperationsEnabled = false, bool discountsEnabled = false)
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var eventRepository = new InMemoryEventRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var discoveryRepository = new InMemoryDiscoveryRepository(store);
        var restaurantRepository = new InMemoryRestaurantRepository(store);
        var restaurantOperationsRepository = new InMemoryRestaurantOperationsRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var browseService = new EventBrowseService(
            eventRepository,
            restaurantRepository,
            profileRepository,
            discoveryRepository,
            lifecycleService,
            restaurantOperationsRepository,
            new TestFeatureFlagService(restaurantOperationsEnabled, discountsEnabled));

        return new TestServices(clock, eventRepository, profileRepository, discoveryRepository, restaurantOperationsRepository, browseService);
    }

    private sealed record TestServices(
        TestClock Clock,
        IEventRepository EventRepository,
        IProfileRepository ProfileRepository,
        IDiscoveryRepository DiscoveryRepository,
        IRestaurantOperationsRepository RestaurantOperationsRepository,
        EventBrowseService BrowseService);

    private sealed class TestFeatureFlagService(bool restaurantOperationsEnabled, bool discountsEnabled) : IFeatureFlagService
    {
        public bool IsMessagingDirectChatEnabled() => false;

        public bool IsMessagingGroupChatEnabled() => true;

        public bool IsNotificationsPushEnabled() => false;

        public bool IsRestaurantsOperationsEnabled() => restaurantOperationsEnabled;

        public bool IsRestaurantsSlotsEnabled() => restaurantOperationsEnabled;

        public bool IsRestaurantsDiscountsEnabled() => discountsEnabled;

        public bool IsPaymentsCheckoutEnabled() => false;

        public bool IsDiscoveryExperimentalSuggestionsEnabled() => false;
    }
}
