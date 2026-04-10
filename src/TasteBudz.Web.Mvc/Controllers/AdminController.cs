using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Admin-only panel for user management, moderation, and data operations.
/// </summary>
[Authorize(Roles = "Admin,Moderator")]
public sealed class AdminController : Controller
{
    private readonly ModerationApiService moderationApiService;
    private readonly ProfileApiService profileApiService;
    private readonly RestaurantApiService restaurantApiService;
    private readonly UserSessionService userSessionService;

    public AdminController(
        ModerationApiService moderationApiService,
        ProfileApiService profileApiService,
        RestaurantApiService restaurantApiService,
        UserSessionService userSessionService)
    {
        this.moderationApiService = moderationApiService;
        this.profileApiService = profileApiService;
        this.restaurantApiService = restaurantApiService;
        this.userSessionService = userSessionService;
    }

    // GET /Admin/Index
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var reports = await moderationApiService.ListReportsAsync(
                new BrowseModerationReportsQuery { Status = ModerationReportStatus.Pending, PageSize = 50 },
                cancellationToken);

            return View(new AdminIndexViewModel { PendingReports = reports.Items });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
    }

    // GET /Admin/Reports
    [HttpGet]
    public async Task<IActionResult> Reports(
        ModerationReportStatus? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reports = await moderationApiService.ListReportsAsync(
                new BrowseModerationReportsQuery { Status = status, Page = page, PageSize = 20 },
                cancellationToken);

            return View(new AdminReportsViewModel
            {
                Reports = reports.Items,
                CurrentPage = page,
                TotalCount = reports.TotalCount,
                FilterStatus = status,
            });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
    }

    // GET /Admin/ReportDetail/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ReportDetail(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var report = await moderationApiService.GetReportAsync(id, cancellationToken);
            return View(new AdminReportDetailViewModel { Report = report });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
    }

    // POST /Admin/BanUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanUser(
        Guid userId,
        bool permanent,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await moderationApiService.CreateRestrictionAsync(new CreateRestrictionRequest
            {
                SubjectUserId = userId,
                Scope = RestrictionScope.DiscoveryVisibility,
                Reason = reason,
                ExpiresAtUtc = permanent ? null : DateTimeOffset.UtcNow.AddDays(7),
            }, cancellationToken);

            TempData["StatusMessage"] = permanent
                ? "User has been permanently banned."
                : "User has been banned for 7 days.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/ImportRestaurants — triggers live Overpass import, Admin only
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportRestaurants(CancellationToken cancellationToken)
    {
        try
        {
            var result = await restaurantApiService.ImportFromOverpassAsync(cancellationToken);
            TempData["StatusMessage"] = result.Message;
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Import failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}