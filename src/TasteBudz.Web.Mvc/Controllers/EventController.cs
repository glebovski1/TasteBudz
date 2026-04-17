using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;
using TasteBudz.Backend.Modules.Groups;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Handles event browsing, creation, detail, and participation.
/// </summary>
[Authorize]
public sealed class EventController : Controller
{
    private readonly EventApiService eventApiService;
    private readonly RestaurantApiService restaurantApiService;
    private readonly ProfileApiService profileApiService;
    private readonly GroupApiService groupApiService;
    private readonly UserSessionService userSessionService;

    public EventController(
        EventApiService eventApiService,
        RestaurantApiService restaurantApiService,
        ProfileApiService profileApiService,
        GroupApiService groupApiService,
        UserSessionService userSessionService)
    {
        this.eventApiService = eventApiService;
        this.restaurantApiService = restaurantApiService;
        this.profileApiService = profileApiService;
        this.groupApiService = groupApiService;
        this.userSessionService = userSessionService;
    }

    // GET /Event/Index
    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        try
        {
            var allResults = await eventApiService.BrowseAsync(
                new BrowseEventsQuery { Q = q, PageSize = 100 },
                cancellationToken);

            // Filter out cancelled events entirely.
            // Hide completed events after 7 days so the list stays clean.
            var completedCutoff = DateTimeOffset.UtcNow.AddDays(-7);
            var visibleEvents = allResults.Items
                .Where(e => e.Status != EventStatus.Cancelled)
                .Where(e => e.Status != EventStatus.Completed || e.EventStartAtUtc >= completedCutoff)
                .ToList();

            return View(EventIndexViewModel.FromDto(visibleEvents, q));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            return View(EventIndexViewModel.Empty);
        }
    }

    // GET /Event/CreateEvent
    [HttpGet]
    public async Task<IActionResult> CreateEvent(CancellationToken cancellationToken)
    {
        var model = await BuildCreateViewModelAsync(new EventCreateViewModel(), cancellationToken);
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
            TempData["StatusMessage"] = $"Event \"{detail.Title ?? "Untitled"}\" created!";
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
            var currentUserId = GetCurrentUserId();
            var isHostOfClosedEvent = detail.HostUserId == currentUserId &&
                                      detail.EventType == EventType.Closed &&
                                      detail.Status is not EventStatus.Cancelled and not EventStatus.Completed;

            // Only fetch the extra invite data the host of an active closed event needs.
            IReadOnlyList<TasteBudz.Backend.Modules.Discovery.BudConnectionDto> budz = [];
            IReadOnlyList<InvitableGroup> invitableGroups = [];

            if (isHostOfClosedEvent)
            {
                try { budz = (await profileApiService.ListBudzAsync(cancellationToken)).ToList(); }
                catch { /* non-fatal — invite panel still renders with groups */ }

                try
                {
                    var myGroups = await profileApiService.ListMyGroupsAsync(cancellationToken);
                    var groupDetails = new List<InvitableGroup>();

                    foreach (var g in myGroups)
                    {
                        try
                        {
                            var gDetail = await groupApiService.GetAsync(g.GroupId, cancellationToken);
                            groupDetails.Add(new InvitableGroup
                            {
                                GroupId = gDetail.GroupId,
                                Name = gDetail.Name,
                                Members = gDetail.Members
                                    .Where(m => m.State == GroupMemberState.Active && m.UserId != currentUserId)
                                    .ToList(),
                            });
                        }
                        catch { /* skip groups that fail to load */ }
                    }

                    invitableGroups = groupDetails;
                }
                catch { /* non-fatal */ }
            }

            return View(EventDetailViewModel.FromDto(detail, participants, currentUserId, budz, invitableGroups));
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
        if (usernames == null || usernames.Count == 0)
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

            TempData["StatusMessage"] = "You have accepted the invitation!";
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<EventCreateViewModel> BuildCreateViewModelAsync(
        EventCreateViewModel model,
        CancellationToken cancellationToken)
    {
        // Fetch user preferences so we can pre-fill cuisine and filter the map.
        string? preferredCuisine = null;
        try
        {
            var prefs = await profileApiService.GetMyPreferencesAsync(cancellationToken);
            preferredCuisine = prefs.CuisineTags.FirstOrDefault();
        }
        catch { /* preferences are optional — continue without them */ }

        // Pre-fill CuisineTarget only when the user hasn't already typed something.
        var cuisineTarget = model.CuisineTarget;
        if (string.IsNullOrWhiteSpace(cuisineTarget) && preferredCuisine is not null)
            cuisineTarget = preferredCuisine;

        try
        {
            // Filter restaurants by the user's first preferred cuisine when available.
            // This keeps the map payload small; the user can clear the filter to see all.
            var restaurants = await restaurantApiService.BrowseAsync(
                new BrowseRestaurantsQuery
                {
                    Cuisine = preferredCuisine,
                    PageSize = 200,
                },
                cancellationToken: cancellationToken);

            return model with
            {
                CuisineTarget = cuisineTarget,
                Restaurants = restaurants.Items
                    .Select(RestaurantPickerItem.FromDto)
                    .ToList(),
            };
        }
        catch
        {
            return model with { CuisineTarget = cuisineTarget, Restaurants = [] };
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