// Integration tests for persistent-group HTTP workflows.
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Exercises the group browse, invite, and group-linked event flows through the HTTP pipeline.
/// </summary>
public sealed class GroupsApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task PrivateGroupInviteFlow_SupportsInviteAcceptAndDetailAccess()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Private Supper Club",
            Visibility = GroupVisibility.Private,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

        var inviteResponse = await ownerClient.PostAsJsonAsync($"/api/v1/groups/{created!.GroupId}/invites", new InviteUserToGroupRequest
        {
            Username = "guest",
        });
        var invite = await inviteResponse.Content.ReadFromJsonAsync<GroupInviteDto>(ApiTestHelpers.JsonOptions);

        var acceptResponse = await guestClient.PatchAsJsonAsync($"/api/v1/groups/invites/{invite!.InviteId}", new RespondToGroupInviteRequest
        {
            Status = GroupInviteStatus.Accepted,
        });
        var detailResponse = await guestClient.GetAsync($"/api/v1/groups/{created.GroupId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.True(detail!.IsCurrentUserMember);
        Assert.Contains(detail.Members, member => member.UserId == guestSession.CurrentUser.UserId && member.State == GroupMemberState.Active);
    }

    [Fact]
    public async Task PublicGroupBrowseAndLinkedEvents_ReturnExpectedResults()
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
            Name = "Public Foodies",
            Visibility = GroupVisibility.Public,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

        var createEventResponse = await ownerClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Linked brunch",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            Capacity = 4,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            GroupId = group!.GroupId,
        });
        createEventResponse.EnsureSuccessStatusCode();

        var browseResponse = await guestClient.GetAsync("/api/v1/groups?q=Public&pageSize=10");
        var browse = await browseResponse.Content.ReadFromJsonAsync<ListResponse<GroupSummaryDto>>(ApiTestHelpers.JsonOptions);

        var groupEventsResponse = await guestClient.GetAsync($"/api/v1/groups/{group.GroupId}/events");
        var groupEvents = await groupEventsResponse.Content.ReadFromJsonAsync<ListResponse<EventSummaryDto>>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, browseResponse.StatusCode);
        Assert.Contains(browse!.Items, item => item.GroupId == group.GroupId);
        Assert.Equal(HttpStatusCode.OK, groupEventsResponse.StatusCode);
        Assert.Contains(groupEvents!.Items, item => item.GroupId == group.GroupId && item.Title == "Linked brunch");
    }

    [Fact]
    public async Task EventCreate_WithNonOwnerGroupId_ReturnsForbidden()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var memberSession = await ApiTestHelpers.RegisterAsync(memberClient, username: "member", email: "member@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(memberClient, memberSession.AccessToken);

        var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Owners only link",
            Visibility = GroupVisibility.Public,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);
        await memberClient.PostAsync($"/api/v1/groups/{group!.GroupId}/members", null);

        var response = await memberClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Unauthorized link",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            Capacity = 4,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            GroupId = group.GroupId,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
