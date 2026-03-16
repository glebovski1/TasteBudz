using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over group browse, detail, membership, invite, and linked-event endpoints.
/// </summary>
public sealed class GroupApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public GroupApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    public Task<ListResponse<GroupSummaryDto>> BrowseAsync(
        BrowseGroupsQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ListResponse<GroupSummaryDto>>(
            BuildBrowsePath(query ?? new BrowseGroupsQuery()),
            cancellationToken);

    public Task<GroupDetailDto> CreateAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreateGroupRequest, GroupDetailDto>(
            "/api/v1/groups",
            request,
            cancellationToken: cancellationToken);

    public Task<GroupDetailDto> GetAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<GroupDetailDto>($"/api/v1/groups/{groupId}", cancellationToken);

    public Task<GroupDetailDto> UpdateAsync(
        Guid groupId,
        UpdateGroupRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdateGroupRequest, GroupDetailDto>(
            $"/api/v1/groups/{groupId}",
            request,
            cancellationToken);

    public Task<ListResponse<EventSummaryDto>> ListGroupEventsAsync(
        Guid groupId,
        GroupEventsQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ListResponse<EventSummaryDto>>(
            BuildGroupEventsPath(groupId, query ?? new GroupEventsQuery()),
            cancellationToken);

    public Task<GroupDetailDto> JoinAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<GroupDetailDto>(
            $"/api/v1/groups/{groupId}/members",
            cancellationToken: cancellationToken);

    public Task LeaveAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync($"/api/v1/groups/{groupId}/members/me", cancellationToken: cancellationToken);

    public Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync(
            $"/api/v1/groups/{groupId}/members/{userId}/removal",
            cancellationToken: cancellationToken);

    public Task<GroupInviteDto> InviteAsync(
        Guid groupId,
        InviteUserToGroupRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<InviteUserToGroupRequest, GroupInviteDto>(
            $"/api/v1/groups/{groupId}/invites",
            request,
            cancellationToken: cancellationToken);

    public Task<GroupInviteDto> RespondToInviteAsync(
        Guid inviteId,
        RespondToGroupInviteRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<RespondToGroupInviteRequest, GroupInviteDto>(
            $"/api/v1/groups/invites/{inviteId}",
            request,
            cancellationToken);

    private static string BuildBrowsePath(BrowseGroupsQuery query)
    {
        var builder = new QueryBuilder();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            builder.Add("q", query.Q);
        }

        builder.Add("page", query.Page.ToString(CultureInfo.InvariantCulture));
        builder.Add("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture));

        return $"/api/v1/groups{builder.ToQueryString()}";
    }

    private static string BuildGroupEventsPath(Guid groupId, GroupEventsQuery query)
    {
        var builder = new QueryBuilder
        {
            { "page", query.Page.ToString(CultureInfo.InvariantCulture) },
            { "pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture) },
        };

        return $"/api/v1/groups/{groupId}/events{builder.ToQueryString()}";
    }
}
