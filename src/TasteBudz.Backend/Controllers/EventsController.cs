// HTTP endpoints for event browse, lifecycle, participation, and invites.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Messaging;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/events")]
/// <summary>
/// Exposes the event workflows implemented for the MVP.
/// </summary>
public sealed class EventsController(
    EventBrowseService eventBrowseService,
    EventService eventService,
    EventParticipationService eventParticipationService,
    EventInviteService eventInviteService,
    MessagingService messagingService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    public Task<ListResponse<EventSummaryDto>> Browse([FromQuery] BrowseEventsQuery query, CancellationToken cancellationToken) =>
        eventBrowseService.BrowseAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, query, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<EventDetailDto>> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var detail = await eventService.CreateAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { eventId = detail.EventId }, detail);
    }

    [HttpGet("{eventId:guid}")]
    public Task<EventDetailDto> Get(Guid eventId, CancellationToken cancellationToken) =>
        eventService.GetAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, eventId, cancellationToken);

    [HttpPatch("{eventId:guid}")]
    public Task<EventDetailDto> Update(Guid eventId, [FromBody] UpdateEventRequest request, CancellationToken cancellationToken) =>
        eventService.UpdateAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, eventId, request, cancellationToken);

    [HttpGet("{eventId:guid}/participants")]
    public Task<IReadOnlyCollection<EventParticipantDto>> ListParticipants(Guid eventId, CancellationToken cancellationToken) =>
        eventService.ListParticipantsAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, eventId, cancellationToken);

    [HttpGet("{eventId:guid}/messages")]
    public Task<CursorPageResponse<ChatMessageDto>> ListMessages(Guid eventId, [FromQuery] ChatHistoryQuery query, CancellationToken cancellationToken) =>
        messagingService.ListEventMessagesAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, eventId, query, cancellationToken);

    [HttpPost("{eventId:guid}/participants")]
    public Task<EventParticipantDto> Join(Guid eventId, CancellationToken cancellationToken) =>
        eventParticipationService.JoinOpenEventAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, cancellationToken);

    [HttpPatch("{eventId:guid}/participants/me")]
    public Task<EventParticipantDto> UpdateMyParticipation(Guid eventId, [FromBody] UpdateMyParticipationRequest request, CancellationToken cancellationToken) =>
        eventParticipationService.UpdateMyParticipationAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, request, cancellationToken);

    [HttpPost("{eventId:guid}/participants/{userId:guid}/removal")]
    public async Task<IActionResult> RemoveParticipant(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        await eventParticipationService.RemoveParticipantAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{eventId:guid}/invites")]
    public Task<IReadOnlyCollection<EventParticipantDto>> Invite(Guid eventId, [FromBody] InviteUsersRequest request, CancellationToken cancellationToken) =>
        eventInviteService.InviteAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, request, cancellationToken);

    [HttpPost("{eventId:guid}/cancellation")]
    public async Task<IActionResult> Cancel(Guid eventId, [FromBody] CancelEventRequest request, CancellationToken cancellationToken)
    {
        await eventService.CancelAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, eventId, request, cancellationToken);
        return NoContent();
    }
}
