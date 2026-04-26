using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class EventMvcTests
{
    [Fact]
    public async Task CreateEvent_RendersGoogleMapsLinksForRestaurantPins()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var restaurantWithPlaceId = Guid.NewGuid();
        var restaurantWithoutPlaceId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?page=1&pageSize=2000",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(
                    new[]
                    {
                        new RestaurantDto(restaurantWithPlaceId, "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Japanese" }, 39.14, -84.51, "google-place-123", 1.2),
                        new RestaurantDto(restaurantWithoutPlaceId, "Taco Corner", "Cincinnati", "OH", "45202", PriceTier.One, new[] { "Mexican" }, 39.10, -84.50, null, 2.4),
                        new RestaurantDto(Guid.NewGuid(), "OpenStreetMap Bistro", "Cincinnati", "OH", "45206", PriceTier.Two, new[] { "American" }, 39.13, -84.48, "osm:987654321", 2.8),
                        new RestaurantDto(Guid.NewGuid(), "<img src=x onerror=alert(1)>", "Cincinnati", "OH", "45206", PriceTier.Two, new[] { "American" }, 39.12, -84.47, null, 3.1),
                    },
                    4)));

        using var response = await client.GetAsync("/Event/CreateEvent");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Open in Google Maps", html);
        Assert.Contains("Pins load only for the current map area once you zoom in.", html);
        Assert.Contains("query_place_id=google-place-123", html);
        Assert.Contains("Taco%20Corner%2C%20Cincinnati%2C%20OH%2045202", html);
        Assert.Contains("OpenStreetMap%20Bistro%2C%20Cincinnati%2C%20OH%2045206", html);
        Assert.DoesNotContain("onclick=\"window.selectRestaurant", html);
        Assert.DoesNotContain("<img src=x onerror=alert(1)>", html);
        Assert.DoesNotContain("query_place_id=osm", html);
        Assert.DoesNotContain("query_place_id=osm%3A", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task CreateEvent_LoadsRestaurantsBeyondTheFirstCatalogPage()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?page=1&pageSize=2000",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(
                    new[]
                    {
                        new RestaurantDto(Guid.NewGuid(), "First Page Ramen", "Cincinnati", "OH", "45220", PriceTier.Two, new[] { "Japanese" }, 39.14, -84.51, null, null),
                    },
                    2)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?page=2&pageSize=2000",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(
                    new[]
                    {
                        new RestaurantDto(Guid.NewGuid(), "Second Page Sushi", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Sushi" }, 39.13, -84.5, null, null),
                    },
                    2)));

        using var response = await client.GetAsync("/Event/CreateEvent");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("First Page Ramen", html);
        Assert.Contains("Second Page Sushi", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventIndex_UsesProfileZipAndAvailabilityFiltersWhenRequested()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/profiles/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateProfile(zipCode: "45220")));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/recurring",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new RecurringAvailabilityWindowDto(Guid.NewGuid(), DayOfWeek.Friday, new TimeOnly(18, 0), new TimeOnly(21, 0), "Dinner"),
                }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/one-off",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<OneOffAvailabilityWindowDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/events?q=ramen&zipCode=45220&radiusMiles=25&availabilityOnly=true&page=1&pageSize=100",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<EventSummaryDto>(
                    new[]
                    {
                        new EventSummaryDto(
                            eventId,
                            "Ramen Meetup",
                            EventType.Open,
                            EventStatus.Open,
                            new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                            new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                            6,
                            3,
                            Guid.NewGuid(),
                            null,
                            "Ramen",
                            null),
                    },
                    1)));

        using var response = await client.GetAsync("/Event/Index?q=ramen&useMyZip=true&radiusMiles=25&availabilityOnly=true");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ramen Meetup", html);
        Assert.Contains("Near my ZIP 45220", html);
        Assert.Contains("Matches my availability", html);
        Assert.Contains("Standalone events", html);
        Assert.DoesNotContain("Ordinary event", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventIndex_PassesStatusAndGroupScopeFiltersToBackend()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/profiles/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateProfile(zipCode: "45220")));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/events?status=Completed&groupLinked=true&page=1&pageSize=100",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<EventSummaryDto>(
                    new[]
                    {
                        new EventSummaryDto(
                            eventId,
                            "Completed group noodles",
                            EventType.Open,
                            EventStatus.Completed,
                            new DateTimeOffset(2026, 4, 20, 19, 0, 0, TimeSpan.Zero),
                            new DateTimeOffset(2026, 4, 20, 17, 0, 0, TimeSpan.Zero),
                            6,
                            4,
                            Guid.NewGuid(),
                            null,
                            "Noodles",
                            groupId),
                    },
                    1)));

        using var response = await client.GetAsync("/Event/Index?status=Completed&eventScope=group");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Completed group noodles", html);
        Assert.Contains("Completed events", html);
        Assert.Contains("Group events", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventIndex_WhenAvailabilityFilterHasNoSavedWindows_ShowsSetupCtaAndSkipsBrowse()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/profiles/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateProfile(zipCode: "45220")));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/recurring",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RecurringAvailabilityWindowDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/one-off",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<OneOffAvailabilityWindowDto>()));

        using var response = await client.GetAsync("/Event/Index?availabilityOnly=true");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Add at least one availability window before using this filter.", html);
        Assert.Contains("/Profile/Availability", html);
        Assert.DoesNotContain(factory.BackendHandler.Requests, request =>
            request.PathAndQuery.StartsWith("/api/v1/events", StringComparison.Ordinal));
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task QuickSearch_UsesRecommendationModeAndOnlyShowsOpenEventsWithSeats()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/profiles/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateProfile(zipCode: "45220")));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/events?status=Open&eventType=Open&recommended=true&page=1&pageSize=100",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<EventSummaryDto>(
                    new[]
                    {
                        new EventSummaryDto(
                            eventId,
                            "Buddy Sushi Night",
                            EventType.Open,
                            EventStatus.Open,
                            new DateTimeOffset(2026, 5, 2, 19, 0, 0, TimeSpan.Zero),
                            new DateTimeOffset(2026, 5, 2, 17, 0, 0, TimeSpan.Zero),
                            6,
                            3,
                            Guid.NewGuid(),
                            null,
                            "Sushi",
                            null,
                            2.1,
                            1,
                            1),
                        new EventSummaryDto(
                            Guid.NewGuid(),
                            "Full table should not appear",
                            EventType.Open,
                            EventStatus.Full,
                            new DateTimeOffset(2026, 5, 2, 20, 0, 0, TimeSpan.Zero),
                            new DateTimeOffset(2026, 5, 2, 18, 0, 0, TimeSpan.Zero),
                            2,
                            2,
                            Guid.NewGuid(),
                            null,
                            "Ramen",
                            null),
                    },
                    2)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/preferences/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreatePreferences(cuisines: new[] { "Sushi" })));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/budz",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new BudConnectionDto(Guid.NewGuid(), "sam", "Sam Carter", DateTimeOffset.UtcNow),
                }));

        using var response = await client.GetAsync("/Event/QuickSearch");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Quick Event Search", html);
        Assert.Contains("Recommended for You", html);
        Assert.Contains("Buddy Sushi Night", html);
        Assert.Contains("2.1 mi away", html);
        Assert.Contains("Matches 1 food preference", html);
        Assert.Contains("1 Bud already joined", html);
        Assert.DoesNotContain("Ordinary event", html);
        Assert.DoesNotContain("Full table should not appear", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventDetails_WhenRestaurantSelected_RendersGoogleMapsLink()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Friday Ramen",
                    EventType.Open,
                    EventStatus.Open,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                    6,
                    2,
                    1,
                    hostUserId,
                    restaurantId,
                    null,
                    null,
                    null)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<EventParticipantDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/feedback",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<EventFeedbackDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurants/{restaurantId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantDto(restaurantId, "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Japanese" }, 39.14, -84.51, "google-place-123", 1.2)));

        using var response = await client.GetAsync($"/Event/EventDetails?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Friday Ramen", html);
        Assert.Contains("Public event", html);
        Assert.Contains("Ramen House", html);
        Assert.Contains("Open in Google Maps", html);
        Assert.Contains("query_place_id=google-place-123", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventDetails_ForClosedHost_RendersDirectUsernameInvite()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Invite-only ramen",
                    EventType.Closed,
                    EventStatus.Open,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                    6,
                    2,
                    1,
                    session.CurrentUser.UserId,
                    null,
                    "Ramen",
                    null,
                    null)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventParticipantDto(session.CurrentUser.UserId, "alex", "Alex Carter", EventParticipantState.Joined, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/feedback",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<EventFeedbackDto>()));
        EnqueueEmptyEventMessages(factory, eventId);
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/budz",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<BudConnectionDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/groups",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<DashboardGroupSummaryDto>()));

        using var response = await client.GetAsync($"/Event/EventDetails?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invite by username", html);
        Assert.Contains("id=\"DirectInviteUsername\"", html);
        Assert.Contains("You can still invite someone by exact username.", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventDetails_ForPublicHost_RendersDirectUsernameInvite()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Open sushi night",
                    EventType.Open,
                    EventStatus.Open,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                    6,
                    2,
                    1,
                    session.CurrentUser.UserId,
                    null,
                    "Sushi",
                    null,
                    null)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventParticipantDto(session.CurrentUser.UserId, "alex", "Alex Carter", EventParticipantState.Joined, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/feedback",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<EventFeedbackDto>()));
        EnqueueEmptyEventMessages(factory, eventId);
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/budz",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<BudConnectionDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/groups",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<DashboardGroupSummaryDto>()));

        using var response = await client.GetAsync($"/Event/EventDetails?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invite by username", html);
        Assert.Contains("id=\"DirectInviteUsername\"", html);
        Assert.Contains("You can still invite someone by exact username.", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventDetails_ForPublicInvitee_RendersInviteActionWithoutDuplicateJoinPanel()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Open invited dinner",
                    EventType.Open,
                    EventStatus.Open,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                    6,
                    2,
                    1,
                    hostUserId,
                    null,
                    "Sushi",
                    null,
                    null)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventParticipantDto(session.CurrentUser.UserId, "alex", "Alex Carter", EventParticipantState.Invited, DateTimeOffset.UtcNow, null, null),
                }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/feedback",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<EventFeedbackDto>()));

        using var response = await client.GetAsync($"/Event/EventDetails?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Accept Invitation", html);
        Assert.DoesNotContain("Join this Event", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventDetails_ForHostWithReservableSlots_RendersReservationAction()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Friday Ramen",
                    EventType.Open,
                    EventStatus.Open,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                    4,
                    2,
                    1,
                    session.CurrentUser.UserId,
                    restaurantId,
                    null,
                    null,
                    null)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<EventParticipantDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/feedback",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<EventFeedbackDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurants/{restaurantId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantDto(restaurantId, "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Japanese" }, 39.14, -84.51, null, 1.2)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurants/{restaurantId}/slots",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new RestaurantSlotDto(slotId, restaurantId, new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 1, 22, 0, 0, TimeSpan.Zero), 4, new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero), 2, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null),
                }));
        EnqueueEmptyEventMessages(factory, eventId);
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/budz",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<BudConnectionDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/groups",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<DashboardGroupSummaryDto>()));

        using var response = await client.GetAsync($"/Event/EventDetails?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Reserve a Slot", html);
        Assert.Contains("Reserve", html);
        Assert.Contains(slotId.ToString(), html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventDetails_WhenFeedbackForbiddenStillRendersEvent()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Closed ramen",
                    EventType.Closed,
                    EventStatus.Open,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 4, 30, 19, 0, 0, TimeSpan.Zero),
                    4,
                    2,
                    1,
                    hostUserId,
                    null,
                    "Ramen",
                    null,
                    null)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventParticipantDto(session.CurrentUser.UserId, "alex", "Alex Carter", EventParticipantState.Invited, DateTimeOffset.UtcNow, null, null),
                }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/feedback",
            (_, _) => StubBackendApiHandler.Problem(HttpStatusCode.Forbidden, "Forbidden", "You are not allowed to view feedback for this event."));

        using var response = await client.GetAsync($"/Event/EventDetails?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Closed ramen", html);
        Assert.Contains("Attendees", html);
        Assert.Contains("Accept Invitation", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventDetails_ForCompletedJoinedParticipant_RendersFeedbackSection()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        EnqueueCompletedEventDetails(factory, eventId, hostUserId, session.CurrentUser.UserId, otherUserId, mediaAssetId);

        using var response = await client.GetAsync($"/Event/EventDetails?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Event Feedback", html);
        Assert.Contains("Average rating: 4.0 / 5", html);
        Assert.Contains("Save Feedback", html);
        Assert.Contains("Upload Photo", html);
        Assert.Contains("Report Feedback", html);
        Assert.Contains(mediaAssetId.ToString(), html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task SaveFeedback_PostsPayloadAndRedirectsToDetails()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        EnqueueCompletedEventDetails(factory, eventId, hostUserId, session.CurrentUser.UserId, otherUserId, mediaAssetId);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, $"/Event/EventDetails?eventId={eventId}");
        factory.BackendHandler.Enqueue(
            HttpMethod.Put,
            $"/api/v1/events/{eventId}/feedback/me",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventFeedbackDto(Guid.NewGuid(), eventId, session.CurrentUser.UserId, "alex", "Alex Carter", 5, "Fresh update.", Array.Empty<EventFeedbackPhotoDto>(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));

        using var response = await client.PostAsync(
            "/Event/SaveFeedback",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["EventId"] = eventId.ToString(),
                ["Rating"] = "5",
                ["Text"] = "Fresh update.",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Event/EventDetails?eventId={eventId}", response.Headers.Location?.ToString());
        Assert.Contains(
            "\"rating\":5",
            factory.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}/feedback/me").Body);
        Assert.Contains(
            "\"text\":\"Fresh update.\"",
            factory.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}/feedback/me").Body);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task FeedbackPhoto_ProxiesBackendImageBytes()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var mediaAssetId = Guid.NewGuid();
        var bytes = new byte[] { 9, 8, 7 };

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/media/{mediaAssetId}",
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") },
                },
            });

        using var response = await client.GetAsync($"/Event/FeedbackPhoto?mediaAssetId={mediaAssetId}");
        var actualBytes = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(bytes, actualBytes);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ReportFeedback_PostsExpectedModerationPayload()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        EnqueueCompletedEventDetails(factory, eventId, hostUserId, session.CurrentUser.UserId, otherUserId, mediaAssetId);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, $"/Event/EventDetails?eventId={eventId}");
        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/reports",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ModerationReportDto(Guid.NewGuid(), session.CurrentUser.UserId, ReportTargetType.User, otherUserId, "Event feedback", "Inappropriate", "Details", eventId, otherUserId, null, DateTimeOffset.UtcNow, ModerationReportStatus.Pending, null, null, null, null)));

        using var response = await client.PostAsync(
            "/Event/ReportFeedback",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["eventId"] = eventId.ToString(),
                ["authorUserId"] = otherUserId.ToString(),
                ["reason"] = "Inappropriate",
                ["explanation"] = "Details",
            }));

        var body = factory.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/reports").Body;
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("\"targetType\":\"User\"", body);
        Assert.Contains($"\"targetId\":\"{otherUserId}", body);
        Assert.Contains($"\"relatedEventId\":\"{eventId}", body);
        Assert.Contains($"\"relatedUserId\":\"{otherUserId}", body);
        factory.BackendHandler.AssertDrained();
    }

    private static void EnqueueCompletedEventDetails(
        TasteBudzMvcFactory factory,
        Guid eventId,
        Guid hostUserId,
        Guid currentUserId,
        Guid otherUserId,
        Guid mediaAssetId)
    {
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Completed ramen",
                    EventType.Open,
                    EventStatus.Completed,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                    6,
                    2,
                    2,
                    hostUserId,
                    null,
                    "Ramen",
                    null,
                    null)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventParticipantDto(currentUserId, "alex", "Alex Carter", EventParticipantState.Joined, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                    new EventParticipantDto(otherUserId, "sam", "Sam Carter", EventParticipantState.Joined, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/feedback",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventFeedbackDto(
                        Guid.NewGuid(),
                        eventId,
                        currentUserId,
                        "alex",
                        "Alex Carter",
                        5,
                        "Great table.",
                        new[] { new EventFeedbackPhotoDto(mediaAssetId, "table.png", "image/png", 3, DateTimeOffset.UtcNow) },
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow),
                    new EventFeedbackDto(
                        Guid.NewGuid(),
                        eventId,
                        otherUserId,
                        "sam",
                        "Sam Carter",
                        3,
                        "Food was late.",
                        Array.Empty<EventFeedbackPhotoDto>(),
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow),
                }));
    }

    private static void EnqueueEmptyEventMessages(TasteBudzMvcFactory factory, Guid eventId)
    {
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/messages?pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(Array.Empty<ChatMessageDto>(), null)));
    }
}
