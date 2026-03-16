using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class GroupApiServiceTests
{
    [Fact]
    public async Task BrowseDetailAndLinkedEvents_SendExpectedRoutes()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new GroupApiService(client));
        var groupId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/groups?q=foodies&page=3&pageSize=12",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<GroupSummaryDto>(
                    new[]
                    {
                        new GroupSummaryDto(groupId, "Foodies", "Dinner club", GroupVisibility.Public, 8),
                    },
                    1)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/groups/{groupId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new GroupDetailDto(
                    groupId,
                    Guid.NewGuid(),
                    "Foodies",
                    "Dinner club",
                    GroupVisibility.Public,
                    GroupLifecycleState.Active,
                    true,
                    new[]
                    {
                        new GroupMemberDto(Guid.NewGuid(), "alex", "Alex Carter", GroupMemberState.Active, DateTimeOffset.UtcNow),
                    })));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/groups/{groupId}/events?page=2&pageSize=8",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<EventSummaryDto>(
                    new[]
                    {
                        new EventSummaryDto(Guid.NewGuid(), "Friday Sushi Night", EventType.Open, EventStatus.Open, new DateTimeOffset(2026, 3, 20, 19, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 20, 17, 0, 0, TimeSpan.Zero), 6, 2, Guid.NewGuid(), null, "Sushi", groupId),
                    },
                    1)));

        var browse = await service.BrowseAsync(new BrowseGroupsQuery
        {
            Q = "foodies",
            Page = 3,
            PageSize = 12,
        });
        var detail = await service.GetAsync(groupId);
        var events = await service.ListGroupEventsAsync(groupId, new GroupEventsQuery
        {
            Page = 2,
            PageSize = 8,
        });

        Assert.Single(browse.Items);
        Assert.Equal(groupId, detail.GroupId);
        Assert.Single(events.Items);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task MutationEndpoints_SendExpectedBodiesRoutesAndDeletes()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new GroupApiService(client));
        var groupId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/groups",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new GroupDetailDto(groupId, Guid.NewGuid(), "Foodies", "Dinner club", GroupVisibility.Public, GroupLifecycleState.Active, true, Array.Empty<GroupMemberDto>())));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/groups/{groupId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new GroupDetailDto(groupId, Guid.NewGuid(), "Updated Foodies", "Updated club", GroupVisibility.Private, GroupLifecycleState.Active, true, Array.Empty<GroupMemberDto>())));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/groups/{groupId}/members",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new GroupDetailDto(groupId, Guid.NewGuid(), "Foodies", "Dinner club", GroupVisibility.Public, GroupLifecycleState.Active, true, Array.Empty<GroupMemberDto>())));
        context.BackendHandler.Enqueue(
            HttpMethod.Delete,
            $"/api/v1/groups/{groupId}/members/me",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/groups/{groupId}/members/{userId}/removal",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/groups/{groupId}/invites",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new GroupInviteDto(inviteId, groupId, userId, "sam", GroupInviteStatus.Pending, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/groups/invites/{inviteId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new GroupInviteDto(inviteId, groupId, userId, "sam", GroupInviteStatus.Accepted, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));

        await service.CreateAsync(new CreateGroupRequest
        {
            Name = "Foodies",
            Description = "Dinner club",
            Visibility = GroupVisibility.Public,
        });
        await service.UpdateAsync(groupId, new UpdateGroupRequest
        {
            Name = "Updated Foodies",
            Description = "Updated club",
            Visibility = GroupVisibility.Private,
        });
        await service.JoinAsync(groupId);
        await service.LeaveAsync(groupId);
        await service.RemoveMemberAsync(groupId, userId);
        await service.InviteAsync(groupId, new InviteUserToGroupRequest
        {
            Username = "sam",
        });
        await service.RespondToInviteAsync(inviteId, new RespondToGroupInviteRequest
        {
            Status = GroupInviteStatus.Accepted,
        });

        Assert.Contains(
            "\"name\":\"Foodies\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/groups").Body);
        Assert.Contains(
            "\"visibility\":\"Private\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/groups/{groupId}" && request.Method == HttpMethod.Patch).Body);
        Assert.Contains(
            "\"username\":\"sam\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/groups/{groupId}/invites").Body);
        Assert.Contains(
            "\"status\":\"Accepted\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/groups/invites/{inviteId}").Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
