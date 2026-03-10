// Integration tests for the current-user dashboard and summary endpoints.
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Verifies that the user-centric summary endpoints compose data from multiple modules correctly.
/// </summary>
public sealed class MeApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task DashboardEndpoints_ReturnCurrentUsersEventsGroupsAndInvites()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Dashboard group",
            Visibility = GroupVisibility.Public,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);
        await guestClient.PostAsync($"/api/v1/groups/{group!.GroupId}/members", null);

        var createOpenEventResponse = await ownerClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Joined event",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });
        var joinedEvent = await createOpenEventResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);
        await guestClient.PostAsync($"/api/v1/events/{joinedEvent!.EventId}/participants", null);

        var createClosedEventResponse = await ownerClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Pending invite",
            EventType = EventType.Closed,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(2),
            Capacity = 4,
            CuisineTarget = "Tacos",
            InviteUsernames = new[] { "guest" },
        });
        createClosedEventResponse.EnsureSuccessStatusCode();

        var dashboardResponse = await guestClient.GetAsync("/api/v1/me/dashboard");
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardDto>(ApiTestHelpers.JsonOptions);
        var eventsResponse = await guestClient.GetAsync("/api/v1/me/events");
        var events = await eventsResponse.Content.ReadFromJsonAsync<DashboardEventSummaryDto[]>(ApiTestHelpers.JsonOptions);
        var groupsResponse = await guestClient.GetAsync("/api/v1/me/groups");
        var groups = await groupsResponse.Content.ReadFromJsonAsync<DashboardGroupSummaryDto[]>(ApiTestHelpers.JsonOptions);
        var invitesResponse = await guestClient.GetAsync("/api/v1/me/event-invites");
        var invites = await invitesResponse.Content.ReadFromJsonAsync<EventInviteDto[]>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, eventsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, groupsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, invitesResponse.StatusCode);
        Assert.Equal(guestSession.CurrentUser.UserId, dashboard!.Profile.UserId);
        Assert.Contains(dashboard.ActiveEvents, item => item.EventId == joinedEvent.EventId);
        Assert.Contains(events!, item => item.EventId == joinedEvent.EventId);
        Assert.Contains(groups!, item => item.GroupId == group.GroupId);
        Assert.Contains(invites!, item => item.Title == "Pending invite");
    }
}
