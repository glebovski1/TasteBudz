using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Backend.Domain;
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
    private readonly RestaurantApiService restaurantApiService;
    private readonly UserSessionService userSessionService;

    public EventController(
        EventApiService eventApiService,
        RestaurantApiService restaurantApiService,
        UserSessionService userSessionService)
    {
        this.eventApiService = eventApiService;
        this.restaurantApiService = restaurantApiService;
        this.userSessionService = userSessionService;
    }

    // GET /Event/Index
    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        try
        {
            var result = await eventApiService.BrowseAsync(
                new BrowseEventsQuery { Q = q, PageSize = 20 },
                cancellationToken);

            return View(EventIndexViewModel.FromDto(result.Items, q));
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
            var selectedRestaurant = await TryGetSelectedRestaurantAsync(detail.SelectedRestaurantId, cancellationToken);
            var currentUserId = GetCurrentUserId();
            var reservableSlots = detail.HostUserId == currentUserId
                ? await TryGetReservableSlotsAsync(detail, selectedRestaurant, cancellationToken)
                : [];

            return View(EventDetailViewModel.FromDto(detail, participants, currentUserId, selectedRestaurant, reservableSlots));
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
        try
        {
            var restaurants = await restaurantApiService.BrowseAsync(
                new BrowseRestaurantsQuery { PageSize = 2000 },
                cancellationToken);

            return model with
            {
                Restaurants = restaurants.Items
                    .Select(RestaurantPickerItem.FromDto)
                    .ToList()
            };
        }
        catch
        {
            return model with { Restaurants = [] };
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
