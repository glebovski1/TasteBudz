using System.Net;
using System.Text.RegularExpressions;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class GroupMvcTests
{
    [Fact]
    public async Task Index_RendersAllVisibleGroupsWithoutSeparateMyGroupsSection()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var publicGroupId = Guid.NewGuid();
        var memberGroupId = Guid.NewGuid();
        var privateGroupId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/groups",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new DashboardGroupSummaryDto(memberGroupId, "Clifton Supper Club", "Weekday dinner meetups.", GroupVisibility.Public, 4),
                    new DashboardGroupSummaryDto(privateGroupId, "Quiet Table", "Invite-only planning.", GroupVisibility.Private, 2),
                }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/groups?page=1&pageSize=100",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<GroupSummaryDto>(
                    new[]
                    {
                        new GroupSummaryDto(publicGroupId, "Neighborhood Noodles", "Public ramen crew.", GroupVisibility.Public, 5),
                    },
                    1)));

        using var response = await client.GetAsync("/Group/Index");
        var html = await response.Content.ReadAsStringAsync();
        var decodedHtml = WebUtility.HtmlDecode(html);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Clifton Supper Club", decodedHtml);
        Assert.Contains("Quiet Table", decodedHtml);
        Assert.Contains("Neighborhood Noodles", decodedHtml);
        Assert.Contains("You're in", decodedHtml);
        Assert.Contains($"/Messaging/GroupChat?groupId={memberGroupId}", html);
        Assert.Contains($"/Messaging/GroupChat?groupId={privateGroupId}", html);
        Assert.DoesNotContain("My Groups", decodedHtml);
        Assert.DoesNotContain("Your dinner circles", decodedHtml);
        Assert.DoesNotContain("Private groups only appear here for current members.", decodedHtml);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task Notifications_GroupInvite_RendersAcceptAndDeclineActions()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var inviteId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/notifications",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new NotificationDto(
                        Guid.NewGuid(),
                        NotificationType.GroupInviteReceived,
                        "GroupInvite",
                        inviteId,
                        "Casey invited you to Quiet Table.",
                        DateTimeOffset.UtcNow,
                        null),
                }));

        using var response = await client.GetAsync("/Notifications/Index");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"/Notifications/RespondGroupInvite?inviteId={inviteId}", html);
        Assert.Contains("Accept", html);
        Assert.Contains("Decline", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task RespondGroupInvite_PostsInviteStatusAndRedirectsToGroupWhenAccepted()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var inviteId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/notifications",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<NotificationDto>()));
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Notifications/Index");
        factory.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/groups/invites/{inviteId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new GroupInviteDto(inviteId, groupId, userId, "alex", GroupInviteStatus.Accepted, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));

        using var response = await client.PostAsync(
            $"/Notifications/RespondGroupInvite?inviteId={inviteId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Status"] = "Accepted",
            }));

        var body = factory.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/groups/invites/{inviteId}").Body;
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Group/Manage?groupId={groupId}", response.Headers.Location?.ToString());
        Assert.Contains("\"status\":\"Accepted\"", body);
        factory.BackendHandler.AssertDrained();
    }

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
                            Guid.NewGuid(),
                            "Sushi night",
                            EventType.Open,
                            EventStatus.Open,
                            new DateTimeOffset(2026, 5, 8, 20, 29, 0, TimeSpan.Zero),
                            new DateTimeOffset(2026, 5, 8, 20, 0, 0, TimeSpan.Zero),
                            6,
                            1,
                            session.CurrentUser.UserId,
                            null,
                            "Sushi",
                            groupId),
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
        Assert.Contains("group-manage-preview-card", html);
        Assert.Contains("group-manage-board-card", html);
        Assert.True(html.IndexOf("group-manage-preview-card", StringComparison.Ordinal) < html.IndexOf("group-manage-board-card", StringComparison.Ordinal));
        Assert.Contains("group-settings-disclosure", html);
        Assert.Contains("group-theme-picker", html);
        Assert.Contains("group-theme-option", html);
        Assert.Contains("Community Board", html);
        Assert.Contains("group-announcement-form--vertical", html);
        Assert.Contains("Ramen plan", html);
        Assert.Contains("Linked Events", html);
        Assert.Contains("Planned Events", html);
        Assert.Contains("History Events", html);
        Assert.Contains("Sushi night", html);
        Assert.Contains("Completed noodles", html);
        Assert.Contains("5.0 / 5 average", html);
        Assert.Contains("Great noodles.", html);
        Assert.Contains("Public event", html);
        Assert.DoesNotContain("Open (public)", html);
        Assert.DoesNotContain("Status: Open", html);
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
        EnqueueGroupDetail(factory, groupId, session.CurrentUser.UserId, GroupVisibility.Public);
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
        Assert.Contains("\"eventType\":\"Open\"", body);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task CreateGroupEvent_ForPrivateGroup_ForcesClosedEventType()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var groupId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        EnqueueGroupEventCreatePage(factory, groupId, session.CurrentUser.UserId, GroupVisibility.Private);
        using var pageResponse = await client.GetAsync($"/Event/CreateEvent?groupId={groupId}");
        var html = await pageResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(html, "name=\"__RequestVerificationToken\".*?value=\"(?<token>[^\"]+)\"", RegexOptions.Singleline);
        var token = WebUtility.HtmlDecode(tokenMatch.Groups["token"].Value);

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.True(tokenMatch.Success);
        Assert.Contains("Private group events are closed and invite-only.", html);
        Assert.Contains("Closed - invite only", html);

        EnqueueGroupDetail(factory, groupId, session.CurrentUser.UserId, GroupVisibility.Private);
        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/events",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Private group ramen",
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
                    groupId,
                    null)));

        using var response = await client.PostAsync(
            "/Event/CreateEvent",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["GroupId"] = groupId.ToString(),
                ["Title"] = "Private group ramen",
                ["EventType"] = "Open",
                ["EventStartAt"] = "2026-05-01T19:00",
                ["Capacity"] = "6",
                ["CuisineTarget"] = "Ramen",
            }));

        var body = factory.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/events").Body;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("\"eventType\":\"Closed\"", body);
        Assert.Contains($"\"groupId\":\"{groupId}", body);
        factory.BackendHandler.AssertDrained();
    }

    private static void EnqueueGroupEventCreatePage(
        TasteBudzMvcFactory factory,
        Guid groupId,
        Guid ownerUserId,
        GroupVisibility visibility = GroupVisibility.Public)
    {
        EnqueueGroupDetail(factory, groupId, ownerUserId, visibility);
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

    private static void EnqueueGroupDetail(
        TasteBudzMvcFactory factory,
        Guid groupId,
        Guid ownerUserId,
        GroupVisibility visibility)
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
                    visibility,
                    GroupWallpaperTheme.Default,
                    GroupLifecycleState.Active,
                    true,
                    new[]
                    {
                        new GroupMemberDto(ownerUserId, "alex", "Alex Carter", null, null, null, null, Array.Empty<string>(), Array.Empty<string>(), GroupMemberState.Active, DateTimeOffset.UtcNow),
                    })));
    }
}
