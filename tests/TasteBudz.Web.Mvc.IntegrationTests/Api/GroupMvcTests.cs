using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class GroupMvcTests
{
    [Fact]
    public async Task Manage_ForOwner_RendersGroupEventCreationAndFeedbackHistory()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var groupId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/groups/{groupId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new GroupDetailDto(
                    groupId,
                    session.CurrentUser.UserId,
                    "Cincy Foodies",
                    "Dinner club",
                    GroupVisibility.Public,
                    GroupWallpaperTheme.Default,
                    GroupLifecycleState.Active,
                    true,
                    new[]
                    {
                        new GroupMemberDto(session.CurrentUser.UserId, "alex", "Alex Carter", null, null, null, null, Array.Empty<string>(), Array.Empty<string>(), GroupMemberState.Active, DateTimeOffset.UtcNow),
                    })));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/groups/{groupId}/events?page=1&pageSize=50",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<EventSummaryDto>(
                    new[]
                    {
                        new EventSummaryDto(
                            eventId,
                            "Completed noodles",
                            EventType.Open,
                            EventStatus.Completed,
                            new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                            new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                            6,
                            3,
                            session.CurrentUser.UserId,
                            null,
                            "Ramen",
                            groupId),
                    },
                    1)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/feedback",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventFeedbackDto(
                        feedbackId,
                        eventId,
                        session.CurrentUser.UserId,
                        "alex",
                        "Alex Carter",
                        5,
                        "Great noodles.",
                        Array.Empty<EventFeedbackPhotoDto>(),
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow),
                }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/groups/{groupId}/announcements",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<GroupAnnouncementDto>(
                    new[]
                    {
                        new GroupAnnouncementDto(
                            Guid.NewGuid(),
                            groupId,
                            session.CurrentUser.UserId,
                            "alex",
                            "Alex Carter",
                            GroupAnnouncementType.OwnerPost,
                            "Ramen plan",
                            "Meet by the front window.",
                            null,
                            DateTimeOffset.UtcNow),
                    },
                    1)));

        using var response = await client.GetAsync($"/Group/Manage?groupId={groupId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Create Group Event", html);
        Assert.Contains($"/Event/CreateEvent?groupId={groupId}", html);
        Assert.Contains("Group Board", html);
        Assert.Contains("Ramen plan", html);
        Assert.Contains("Linked Events", html);
        Assert.Contains("Completed noodles", html);
        Assert.Contains("5.0 / 5 average", html);
        Assert.Contains("Great noodles.", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task CreateGroupEvent_PostsGroupIdInEventCreatePayload()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var groupId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        EnqueueGroupEventCreatePage(factory, groupId, session.CurrentUser.UserId);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, $"/Event/CreateEvent?groupId={groupId}");
        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/events",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Group ramen night",
                    EventType.Open,
                    EventStatus.Open,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                    6,
                    2,
                    1,
                    session.CurrentUser.UserId,
                    null,
                    "Ramen",
                    groupId,
                    null)));

        using var response = await client.PostAsync(
            "/Event/CreateEvent",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["GroupId"] = groupId.ToString(),
                ["Title"] = "Group ramen night",
                ["EventType"] = "Open",
                ["EventStartAt"] = "2026-05-01T19:00",
                ["Capacity"] = "6",
                ["CuisineTarget"] = "Ramen",
            }));

        var body = factory.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/events").Body;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Event/EventDetails?eventId={eventId}", response.Headers.Location?.ToString());
        Assert.Contains($"\"groupId\":\"{groupId}", body);
        factory.BackendHandler.AssertDrained();
    }

    private static void EnqueueGroupEventCreatePage(
        TasteBudzMvcFactory factory,
        Guid groupId,
        Guid ownerUserId)
    {
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/groups/{groupId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new GroupDetailDto(
                    groupId,
                    ownerUserId,
                    "Cincy Foodies",
                    "Dinner club",
                    GroupVisibility.Public,
                    GroupWallpaperTheme.Default,
                    GroupLifecycleState.Active,
                    true,
                    new[]
                    {
                        new GroupMemberDto(ownerUserId, "alex", "Alex Carter", null, null, null, null, Array.Empty<string>(), Array.Empty<string>(), GroupMemberState.Active, DateTimeOffset.UtcNow),
                    })));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?page=1&pageSize=2000",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(
                    new[]
                    {
                        new RestaurantDto(Guid.NewGuid(), "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Two, new[] { "Japanese" }, 39.14, -84.51, null, 1.2),
                    },
                    1)));
    }
}
