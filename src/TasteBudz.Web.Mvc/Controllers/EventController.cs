using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Modules.Events;
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

    // GET /Event/Create
    [HttpGet]
    public async Task<IActionResult> CreateEvent(CancellationToken cancellationToken)
    {
        var model = await BuildCreateViewModelAsync(new EventCreateViewModel(), cancellationToken);
        return View(model);
    }

    // POST /Event/Create
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

    // GET /Event/Detail/{eventId}
    [HttpGet]
    public async Task<IActionResult> EventDetails(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await eventApiService.GetAsync(eventId, cancellationToken);
            var currentUserId = GetCurrentUserId();
            return View(EventDetailViewModel.FromDto(detail, currentUserId));
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<EventCreateViewModel> BuildCreateViewModelAsync(
        EventCreateViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var restaurants = await restaurantApiService.BrowseAsync(cancellationToken: cancellationToken);
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