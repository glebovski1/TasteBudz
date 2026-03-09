// HTTP endpoints for persistent-group workflows.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Messaging;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/groups")]
/// <summary>
/// Exposes public-group browse plus owner/member group workflows.
/// </summary>
public sealed class GroupsController(
    GroupService groupService,
    MessagingService messagingService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    public Task<ListResponse<GroupSummaryDto>> Browse([FromQuery] BrowseGroupsQuery query, CancellationToken cancellationToken) =>
        groupService.BrowseAsync(query, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<GroupDetailDto>> Create([FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var detail = await groupService.CreateAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { groupId = detail.GroupId }, detail);
    }

    [HttpGet("{groupId:guid}")]
    public Task<GroupDetailDto> Get(Guid groupId, CancellationToken cancellationToken) =>
        groupService.GetAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, groupId, cancellationToken);

    [HttpPatch("{groupId:guid}")]
    public Task<GroupDetailDto> Update(Guid groupId, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken) =>
        groupService.UpdateAsync(currentUserAccessor.GetRequiredCurrentUser(), groupId, request, cancellationToken);

    [HttpGet("{groupId:guid}/events")]
    public Task<ListResponse<EventSummaryDto>> ListEvents(Guid groupId, [FromQuery] GroupEventsQuery query, CancellationToken cancellationToken) =>
        groupService.ListGroupEventsAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, groupId, query, cancellationToken);

    [HttpGet("{groupId:guid}/messages")]
    public Task<CursorPageResponse<ChatMessageDto>> ListMessages(Guid groupId, [FromQuery] ChatHistoryQuery query, CancellationToken cancellationToken) =>
        messagingService.ListGroupMessagesAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, groupId, query, cancellationToken);

    [HttpPost("{groupId:guid}/members")]
    public Task<GroupDetailDto> Join(Guid groupId, CancellationToken cancellationToken) =>
        groupService.JoinAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, groupId, cancellationToken);

    [HttpDelete("{groupId:guid}/members/me")]
    public async Task<IActionResult> Leave(Guid groupId, CancellationToken cancellationToken)
    {
        await groupService.LeaveAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, groupId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{groupId:guid}/members/{userId:guid}/removal")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId, CancellationToken cancellationToken)
    {
        await groupService.RemoveMemberAsync(currentUserAccessor.GetRequiredCurrentUser(), groupId, userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{groupId:guid}/invites")]
    public Task<GroupInviteDto> Invite(Guid groupId, [FromBody] InviteUserToGroupRequest request, CancellationToken cancellationToken) =>
        groupService.InviteAsync(currentUserAccessor.GetRequiredCurrentUser(), groupId, request, cancellationToken);

    [HttpPatch("invites/{inviteId:guid}")]
    public Task<GroupInviteDto> RespondToInvite(Guid inviteId, [FromBody] RespondToGroupInviteRequest request, CancellationToken cancellationToken) =>
        groupService.RespondToInviteAsync(currentUserAccessor.GetRequiredCurrentUser(), inviteId, request, cancellationToken);
}
