// Integration tests for persistent-group HTTP workflows.
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Profiles;

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

        var announcementsResponse = await guestClient.GetAsync($"/api/v1/groups/{group.GroupId}/announcements");
        var announcements = await announcementsResponse.Content.ReadFromJsonAsync<ListResponse<GroupAnnouncementDto>>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, announcementsResponse.StatusCode);
        Assert.Contains(announcements!.Items, item =>
            item.AnnouncementType == GroupAnnouncementType.EventCreated &&
            item.RelatedEventId.HasValue &&
            item.Title == "New group event");
    }

    [Fact]
    public async Task GroupDetailAndBrowse_HideFullBannedMembers()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();
        using var browserClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var memberSession = await ApiTestHelpers.RegisterAsync(memberClient, username: "member", email: "member@example.com");
        var browserSession = await ApiTestHelpers.RegisterAsync(browserClient, username: "browser", email: "browser@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(memberClient, memberSession.AccessToken);
        ApiTestHelpers.SetBearer(browserClient, browserSession.AccessToken);

        var createResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Public Foodies",
            Visibility = GroupVisibility.Public,
        });
        var group = await createResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);
        await memberClient.PostAsync($"/api/v1/groups/{group!.GroupId}/members", null);

        var banResponse = await moderatorClient.PostAsJsonAsync("/api/v1/moderation/bans", new CreateUserBanRequest
        {
            SubjectUserId = memberSession.CurrentUser.UserId,
            Reason = "Full safety ban",
        });
        var detailResponse = await ownerClient.GetAsync($"/api/v1/groups/{group.GroupId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);
        var browseResponse = await browserClient.GetAsync("/api/v1/groups?q=Public&pageSize=10");
        var browse = await browseResponse.Content.ReadFromJsonAsync<ListResponse<GroupSummaryDto>>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, banResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.DoesNotContain(detail!.Members, item => item.UserId == memberSession.CurrentUser.UserId);
        Assert.Equal(HttpStatusCode.OK, browseResponse.StatusCode);
        Assert.Contains(browse!.Items, item => item.GroupId == group.GroupId && item.ActiveMembers == 1);
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

    [Fact]
    public async Task GroupEndpoints_EnforceOwnerUpdateAndRemovalRules()
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
            Name = "Owner guardrails",
            Visibility = GroupVisibility.Public,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);
        await guestClient.PostAsync($"/api/v1/groups/{group!.GroupId}/members", null);

        var guestUpdateResponse = await guestClient.PatchAsJsonAsync($"/api/v1/groups/{group.GroupId}", new UpdateGroupRequest
        {
            Name = "Guest update attempt",
        });

        var removeResponse = await ownerClient.PostAsync($"/api/v1/groups/{group.GroupId}/members/{guestSession.CurrentUser.UserId}/removal", null);
        var guestDetailResponse = await guestClient.GetAsync($"/api/v1/groups/{group.GroupId}");
        var guestDetail = await guestDetailResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);
        var rejoinResponse = await guestClient.PostAsync($"/api/v1/groups/{group.GroupId}/members", null);
        var ownerLeaveResponse = await ownerClient.DeleteAsync($"/api/v1/groups/{group.GroupId}/members/me");

        Assert.Equal(HttpStatusCode.Forbidden, guestUpdateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, guestDetailResponse.StatusCode);
        Assert.False(guestDetail!.IsCurrentUserMember);
        Assert.Equal(HttpStatusCode.Forbidden, rejoinResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ownerLeaveResponse.StatusCode);
    }

    [Fact]
    public async Task PrivateGroupInvite_WhenUsersAreBlocked_ReturnsForbidden()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);

        var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Blocked invite group",
            Visibility = GroupVisibility.Private,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

        var blockResponse = await ownerClient.PostAsJsonAsync("/api/v1/blocks", new CreateBlockRequest
        {
            BlockedUserId = guestSession.CurrentUser.UserId,
        });
        var inviteResponse = await ownerClient.PostAsJsonAsync($"/api/v1/groups/{group!.GroupId}/invites", new InviteUserToGroupRequest
        {
            Username = "guest",
        });
        var problem = await inviteResponse.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, blockResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, inviteResponse.StatusCode);
        Assert.Contains("application/problem+json", inviteResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(403, problem!.Status);
        Assert.Equal("Blocking prevents group invitations between these users.", problem.Detail);
    }

    [Fact]
    public async Task GroupAnnouncements_AreOwnerManagedAndVisibleWithGroupAccess()
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
            Name = "Announcement group",
            Visibility = GroupVisibility.Public,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

        var guestPostResponse = await guestClient.PostAsJsonAsync($"/api/v1/groups/{group!.GroupId}/announcements", new CreateGroupAnnouncementRequest
        {
            Title = "Guest post",
            Body = "This should not be accepted.",
        });
        var ownerPostResponse = await ownerClient.PostAsJsonAsync($"/api/v1/groups/{group.GroupId}/announcements", new CreateGroupAnnouncementRequest
        {
            Title = "Friday menu",
            Body = "Trying ramen and dumplings.",
        });
        var listResponse = await guestClient.GetAsync($"/api/v1/groups/{group.GroupId}/announcements");
        var announcements = await listResponse.Content.ReadFromJsonAsync<ListResponse<GroupAnnouncementDto>>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, guestPostResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownerPostResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(announcements!.Items, item =>
            item.AnnouncementType == GroupAnnouncementType.OwnerPost &&
            item.Title == "Friday menu" &&
            item.Body == "Trying ramen and dumplings.");
    }

    [Fact]
    public async Task GroupUpdate_OwnerCanSetPresetWallpaper()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);

        var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Wallpaper group",
            Visibility = GroupVisibility.Public,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

        var updateResponse = await ownerClient.PatchAsJsonAsync($"/api/v1/groups/{group!.GroupId}", new UpdateGroupRequest
        {
            WallpaperTheme = GroupWallpaperTheme.TacoTable,
        });
        var updated = await updateResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(GroupWallpaperTheme.TacoTable, updated!.WallpaperTheme);
    }
}
