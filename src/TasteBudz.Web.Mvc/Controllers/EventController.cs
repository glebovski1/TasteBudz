using Microsoft.AspNetCore.Authorization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Handles event browsing, creation, detail, and participation.
/// </summary>
[Authorize]
public sealed class EventController : Controller
{
    private readonly EventApiService eventApiService;
    private readonly GroupApiService groupApiService;
    private readonly RestaurantApiService restaurantApiService;
    private readonly ProfileApiService profileApiService;
    private readonly ModerationApiService moderationApiService;
    private readonly UserSessionService userSessionService;

    public EventController(
        EventApiService eventApiService,
        GroupApiService groupApiService,
        RestaurantApiService restaurantApiService,
        ProfileApiService profileApiService,
        ModerationApiService moderationApiService,
        UserSessionService userSessionService)
    {
        this.eventApiService = eventApiService;
        this.groupApiService = groupApiService;
        this.restaurantApiService = restaurantApiService;
        this.profileApiService = profileApiService;
        this.moderationApiService = moderationApiService;
        this.userSessionService = userSessionService;
    }

    // GET /Event/Index
    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        bool useMyZip,
        int? radiusMiles,
        bool availabilityOnly,
        CancellationToken cancellationToken)
    {
        var selectedRadiusMiles = radiusMiles is 5 or 10 or 25
            ? radiusMiles.Value
            : EventIndexViewModel.DefaultRadiusMiles;

        try
        {
            var profile = await profileApiService.GetMyProfileAsync(cancellationToken);
            var hasAvailabilityWindows = true;

            if (availabilityOnly)
            {
                var recurring = await profileApiService.ListRecurringAvailabilityAsync(cancellationToken);
                var oneOff = await profileApiService.ListOneOffAvailabilityAsync(cancellationToken);
                hasAvailabilityWindows = recurring.Count > 0 || oneOff.Count > 0;

                if (!hasAvailabilityWindows)
                {
                    return View(EventIndexViewModel.EmptyWithFilters(
                        q,
                        useMyZip,
                        selectedRadiusMiles,
                        availabilityOnly,
                        profile.HomeAreaZipCode,
                        showAvailabilitySetupCta: true));
                }
            }

            var result = await eventApiService.BrowseAsync(
                new BrowseEventsQuery
                {
                    Q = q,
                    ZipCode = useMyZip ? profile.HomeAreaZipCode : null,
                    RadiusMiles = useMyZip ? selectedRadiusMiles : null,
                    AvailabilityOnly = availabilityOnly,
                    PageSize = 100,
                },
                cancellationToken);

            var completedCutoff = DateTimeOffset.UtcNow.AddDays(-7);
            var visibleEvents = result.Items
                .Where(e => e.Status != EventStatus.Cancelled)
                .Where(e => e.Status != EventStatus.Completed || e.EventStartAtUtc >= completedCutoff)
                .ToList();

            return View(EventIndexViewModel.FromDto(
                visibleEvents,
                q,
                useMyZip,
                selectedRadiusMiles,
                availabilityOnly,
                profile.HomeAreaZipCode));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            return View(EventIndexViewModel.EmptyWithFilters(
                q,
                useMyZip,
                selectedRadiusMiles,
                availabilityOnly,
                null));
        }
    }

    // GET /Event/CreateEvent
    [HttpGet]
    public async Task<IActionResult> CreateEvent(Guid? groupId, CancellationToken cancellationToken)
    {
        var model = new EventCreateViewModel { GroupId = groupId };

        if (groupId.HasValue)
        {
            try
            {
                var group = await groupApiService.GetAsync(groupId.Value, cancellationToken);

                if (group.OwnerUserId != GetCurrentUserId())
                {
                    TempData["StatusMessage"] = "Only the group owner can create a group event.";
                    return RedirectToAction(nameof(GroupController.Manage), "Group", new { groupId = groupId.Value });
                }

                model = model with { GroupName = group.Name };
            }
            catch (BackendAuthenticationExpiredException)
            {
                return await RedirectToLoginAsync(cancellationToken);
            }
            catch
            {
                TempData["StatusMessage"] = "That group could not be found.";
                return RedirectToAction(nameof(GroupController.Index), "Group");
            }
        }

        model = await BuildCreateViewModelAsync(model, cancellationToken);
        return View(model);
    }

    // POST /Event/CreateEvent
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model = await BuildCreateViewModelAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var detail = await eventApiService.CreateAsync(model.ToRequest(), cancellationToken);
            TempData["StatusMessage"] = detail.GroupId.HasValue
                ? $"Group event \"{detail.Title ?? "Untitled"}\" created!"
                : $"Event \"{detail.Title ?? "Untitled"}\" created!";
            return RedirectToAction(nameof(EventDetails), new { eventId = detail.EventId });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model = await BuildCreateViewModelAsync(model, cancellationToken);
            return View(model);
        }
    }

    // GET /Event/EventDetails/{eventId}
    [HttpGet]
    public async Task<IActionResult> EventDetails(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await eventApiService.GetAsync(eventId, cancellationToken);
            var participants = await eventApiService.ListParticipantsAsync(eventId, cancellationToken);
            var feedback = await TryListFeedbackAsync(eventId, cancellationToken);
            var selectedRestaurant = await TryGetSelectedRestaurantAsync(detail.SelectedRestaurantId, cancellationToken);
            var currentUserId = GetCurrentUserId();
            var reservableSlots = detail.HostUserId == currentUserId
                ? await TryGetReservableSlotsAsync(detail, selectedRestaurant, cancellationToken)
                : [];
            var isHostOfClosedEvent = detail.HostUserId == currentUserId &&
                                      detail.EventType == EventType.Closed &&
                                      detail.Status is not EventStatus.Cancelled and not EventStatus.Completed;
            IReadOnlyList<BudConnectionDto> budz = [];
            IReadOnlyList<InvitableGroup> invitableGroups = [];

            if (isHostOfClosedEvent)
            {
                try
                {
                    budz = (await profileApiService.ListBudzAsync(cancellationToken)).ToList();
                }
                catch (BackendApiException)
                {
                    budz = [];
                }

                try
                {
                    var myGroups = await profileApiService.ListMyGroupsAsync(cancellationToken);
                    var groupDetails = new List<InvitableGroup>();

                    foreach (var group in myGroups)
                    {
                        try
                        {
                            var groupDetail = await groupApiService.GetAsync(group.GroupId, cancellationToken);
                            groupDetails.Add(new InvitableGroup
                            {
                                GroupId = groupDetail.GroupId,
                                Name = groupDetail.Name,
                                Members = groupDetail.Members
                                    .Where(member => member.State == GroupMemberState.Active && member.UserId != currentUserId)
                                    .ToList(),
                            });
                        }
                        catch (BackendApiException)
                        {
                            // Skip groups that cannot be loaded for invite suggestions.
                        }
                    }

                    invitableGroups = groupDetails;
                }
                catch (BackendApiException)
                {
                    invitableGroups = [];
                }
            }

            return View(EventDetailViewModel.FromDto(
                detail,
                participants,
                currentUserId,
                selectedRestaurant,
                reservableSlots,
                feedback,
                budz,
                invitableGroups));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "That event could not be found.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST /Event/Invite
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(Guid eventId, List<string> usernames, CancellationToken cancellationToken)
    {
        if (usernames is null || usernames.Count == 0)
        {
            TempData["StatusMessage"] = "Please select at least one person to invite.";
            return RedirectToAction(nameof(EventDetails), new { eventId });
        }

        try
        {
            await eventApiService.InviteAsync(
                eventId,
                new InviteUsersRequest { Usernames = usernames },
                cancellationToken);

            TempData["StatusMessage"] = $"{usernames.Count} invitation(s) sent.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not send invites: {ex.Message}";
        }

        return RedirectToAction(nameof(EventDetails), new { eventId });
    }

    // POST /Event/Accept
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            await eventApiService.UpdateMyParticipationAsync(
                eventId,
                new UpdateMyParticipationRequest { State = EventParticipantState.Joined },
                cancellationToken);

            TempData["StatusMessage"] = "You have accepted the invitation.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not accept invite: {ex.Message}";
        }

        return RedirectToAction(nameof(EventDetails), new { eventId });
    }

    // POST /Event/Decline
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            await eventApiService.UpdateMyParticipationAsync(
                eventId,
                new UpdateMyParticipationRequest { State = EventParticipantState.Declined },
                cancellationToken);

            TempData["StatusMessage"] = "Invitation declined.";
            return RedirectToAction(nameof(Index));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not decline invite: {ex.Message}";
            return RedirectToAction(nameof(EventDetails), new { eventId });
        }
    }

    // POST /Event/Join
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            await eventApiService.JoinAsync(eventId, cancellationToken);
            TempData["StatusMessage"] = "You joined the event!";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not join: {ex.Message}";
        }

        return RedirectToAction(nameof(EventDetails), new { eventId });
    }

    // POST /Event/Leave
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            await eventApiService.UpdateMyParticipationAsync(
                eventId,
                new UpdateMyParticipationRequest { State = EventParticipantState.Left },
                cancellationToken);

            TempData["StatusMessage"] = "You have left the event.";
            return RedirectToAction(nameof(Index));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not leave: {ex.Message}";
            return RedirectToAction(nameof(EventDetails), new { eventId });
        }
    }

    // POST /Event/Kick
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Kick(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await eventApiService.RemoveParticipantAsync(eventId, userId, cancellationToken);
            TempData["StatusMessage"] = "Attendee removed.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not remove attendee: {ex.Message}";
        }

        return RedirectToAction(nameof(EventDetails), new { eventId });
    }

    // POST /Event/Cancel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid eventId, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["StatusMessage"] = "A cancellation reason is required.";
            return RedirectToAction(nameof(EventDetails), new { eventId });
        }

        try
        {
            await eventApiService.CancelAsync(
                eventId,
                new CancelEventRequest { Reason = reason.Trim() },
                cancellationToken);

            TempData["StatusMessage"] = "Event cancelled.";
            return RedirectToAction(nameof(Index));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not cancel: {ex.Message}";
            return RedirectToAction(nameof(EventDetails), new { eventId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFeedback(EventFeedbackFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Feedback needs a rating and text.";
            return RedirectToAction(nameof(EventDetails), new { eventId = model.EventId });
        }

        try
        {
            await eventApiService.UpsertFeedbackAsync(model.EventId, model.ToRequest(), cancellationToken);
            TempData["StatusMessage"] = "Feedback saved.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not save feedback: {ex.Message}";
        }

        return RedirectToAction(nameof(EventDetails), new { eventId = model.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadFeedbackPhoto(Guid eventId, IFormFile? photo, CancellationToken cancellationToken)
    {
        if (photo is null || photo.Length == 0)
        {
            TempData["StatusMessage"] = "Choose a photo before uploading.";
            return RedirectToAction(nameof(EventDetails), new { eventId });
        }

        try
        {
            await eventApiService.UploadFeedbackPhotoAsync(eventId, photo, cancellationToken);
            TempData["StatusMessage"] = "Feedback photo uploaded.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not upload feedback photo: {ex.Message}";
        }

        return RedirectToAction(nameof(EventDetails), new { eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFeedbackPhoto(Guid eventId, Guid mediaAssetId, CancellationToken cancellationToken)
    {
        try
        {
            await eventApiService.DeleteFeedbackPhotoAsync(eventId, mediaAssetId, cancellationToken);
            TempData["StatusMessage"] = "Feedback photo removed.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not remove feedback photo: {ex.Message}";
        }

        return RedirectToAction(nameof(EventDetails), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> FeedbackPhoto(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        try
        {
            var media = await eventApiService.GetMediaAsync(mediaAssetId, cancellationToken);
            return File(media.Content, media.ContentType);
        }
        catch (BackendAuthenticationExpiredException)
        {
            return Unauthorized();
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportFeedback(Guid eventId, Guid authorUserId, string reason, string? explanation, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["StatusMessage"] = "A report reason is required.";
            return RedirectToAction(nameof(EventDetails), new { eventId });
        }

        try
        {
            await moderationApiService.CreateReportAsync(
                new CreateModerationReportRequest
                {
                    TargetType = ReportTargetType.User,
                    TargetId = authorUserId,
                    Category = "Event feedback",
                    Reason = reason.Trim(),
                    Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim(),
                    RelatedEventId = eventId,
                    RelatedUserId = authorUserId,
                },
                cancellationToken);
            TempData["StatusMessage"] = "Feedback report submitted.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not submit report: {ex.Message}";
        }

        return RedirectToAction(nameof(EventDetails), new { eventId });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<EventCreateViewModel> BuildCreateViewModelAsync(
        EventCreateViewModel model,
        CancellationToken cancellationToken)
    {
        var groupName = model.GroupName;

        if (model.GroupId.HasValue && string.IsNullOrWhiteSpace(groupName))
        {
            try
            {
                var group = await groupApiService.GetAsync(model.GroupId.Value, cancellationToken);
                groupName = group.Name;
            }
            catch
            {
                groupName = null;
            }
        }

        try
        {
            var restaurants = await restaurantApiService.BrowseAsync(
                new BrowseRestaurantsQuery { PageSize = 2000 },
                cancellationToken);

            return model with
            {
                GroupName = groupName,
                Restaurants = restaurants.Items
                    .Select(RestaurantPickerItem.FromDto)
                    .ToList(),
            };
        }
        catch
        {
            return model with { GroupName = groupName, Restaurants = [] };
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReserveSlot(Guid eventId, Guid slotId, CancellationToken cancellationToken)
    {
        try
        {
            await eventApiService.ReserveSlotAsync(
                eventId,
                new ReserveEventSlotRequest { SlotId = slotId },
                cancellationToken);
            TempData["StatusMessage"] = "Restaurant slot reserved.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not reserve slot: {ex.Message}";
        }

        return RedirectToAction(nameof(EventDetails), new { eventId });
    }

    private async Task<RestaurantDto?> TryGetSelectedRestaurantAsync(
        Guid? selectedRestaurantId,
        CancellationToken cancellationToken)
    {
        if (!selectedRestaurantId.HasValue)
        {
            return null;
        }

        try
        {
            return await restaurantApiService.GetAsync(selectedRestaurantId.Value, cancellationToken);
        }
        catch (BackendAuthenticationExpiredException)
        {
            throw;
        }
        catch (BackendApiException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyCollection<EventFeedbackDto>> TryListFeedbackAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await eventApiService.ListFeedbackAsync(eventId, cancellationToken);
        }
        catch (BackendAuthenticationExpiredException)
        {
            throw;
        }
        catch (BackendApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return [];
        }
    }

    private async Task<IReadOnlyCollection<RestaurantSlotDto>> TryGetReservableSlotsAsync(
        EventDetailDto detail,
        RestaurantDto? selectedRestaurant,
        CancellationToken cancellationToken)
    {
        if (selectedRestaurant is null || detail.SlotReservation is not null)
        {
            return [];
        }

        try
        {
            return await restaurantApiService.ListReservableSlotsAsync(selectedRestaurant.RestaurantId, cancellationToken);
        }
        catch (BackendAuthenticationExpiredException)
        {
            throw;
        }
        catch (BackendApiException)
        {
            return [];
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
