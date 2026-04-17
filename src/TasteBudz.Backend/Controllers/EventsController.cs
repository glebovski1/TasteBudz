// HTTP endpoints for event browse, lifecycle, participation, and invites.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Restaurants;

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
    EventFeedbackService eventFeedbackService,
    EventSlotReservationService eventSlotReservationService,
    MessagingService messagingService,
    IFeatureFlagService featureFlagService,
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

    [HttpGet("{eventId:guid}/feedback")]
    public Task<IReadOnlyCollection<EventFeedbackDto>> ListFeedback(Guid eventId, CancellationToken cancellationToken) =>
        eventFeedbackService.ListAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, cancellationToken);

    [HttpPut("{eventId:guid}/feedback/me")]
    public Task<EventFeedbackDto> UpsertMyFeedback(Guid eventId, [FromBody] UpsertEventFeedbackRequest request, CancellationToken cancellationToken) =>
        eventFeedbackService.UpsertMineAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, request, cancellationToken);

    [HttpPost("{eventId:guid}/feedback/me/photos")]
    [Consumes("multipart/form-data")]
    public Task<EventFeedbackPhotoDto> UploadMyFeedbackPhoto(Guid eventId, [FromForm] UploadImageRequest request, CancellationToken cancellationToken) =>
        eventFeedbackService.UploadMyPhotoAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, request, cancellationToken);

    [HttpDelete("{eventId:guid}/feedback/me/photos/{mediaAssetId:guid}")]
    public async Task<IActionResult> DeleteMyFeedbackPhoto(Guid eventId, Guid mediaAssetId, CancellationToken cancellationToken)
    {
        await eventFeedbackService.DeleteMyPhotoAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, mediaAssetId, cancellationToken);
        return NoContent();
    }

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

    [HttpPost("{eventId:guid}/slot-reservations")]
    public Task<EventSlotReservationDto> ReserveSlot(
        Guid eventId,
        [FromBody] ReserveEventSlotRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSlotsEnabled();
        return eventSlotReservationService.ReserveAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, request, cancellationToken);
    }

    private void EnsureSlotsEnabled()
    {
        if (!featureFlagService.IsRestaurantsOperationsEnabled() ||
            !featureFlagService.IsRestaurantsSlotsEnabled())
        {
            throw ApiException.NotFound("Restaurant slots are not enabled.");
        }
    }
}
