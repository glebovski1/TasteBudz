using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Admin-only panel for user management, moderation, and catalog maintenance.
/// </summary>
[Authorize(Roles = "Admin,Moderator")]
public sealed class AdminController : Controller
{
    private readonly ModerationApiService moderationApiService;
    private readonly ProfileApiService profileApiService;
    private readonly RestaurantApiService restaurantApiService;
    private readonly AuthApiService authApiService;
    private readonly MessagingApiService messagingApiService;
    private readonly UserSessionService userSessionService;

    public AdminController(
        ModerationApiService moderationApiService,
        ProfileApiService profileApiService,
        RestaurantApiService restaurantApiService,
        AuthApiService authApiService,
        MessagingApiService messagingApiService,
        UserSessionService userSessionService)
    {
        this.moderationApiService = moderationApiService;
        this.profileApiService = profileApiService;
        this.restaurantApiService = restaurantApiService;
        this.authApiService = authApiService;
        this.messagingApiService = messagingApiService;
        this.userSessionService = userSessionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            return View(await BuildIndexViewModelAsync(cancellationToken));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GeneratePasswordResetToken(string? usernameOrEmail, Guid? passwordResetRequestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) && !passwordResetRequestId.HasValue)
        {
            TempData["StatusMessage"] = "Username or email is required.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var resetToken = await authApiService.CreatePasswordResetTokenAsync(
                new CreatePasswordResetTokenRequest
                {
                    UsernameOrEmail = string.IsNullOrWhiteSpace(usernameOrEmail) ? null : usernameOrEmail.Trim(),
                    PasswordResetRequestId = passwordResetRequestId,
                },
                cancellationToken);
            var model = (await BuildIndexViewModelAsync(cancellationToken)) with
            {
                GeneratedPasswordResetToken = resetToken,
            };

            TempData["StatusMessage"] = passwordResetRequestId.HasValue
                ? "Password reset link generated and request closed."
                : "Password reset link generated.";
            return View("Index", model);
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Reset token failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DismissPasswordResetRequest(Guid requestId, CancellationToken cancellationToken)
    {
        try
        {
            await authApiService.ClosePasswordResetRequestAsync(requestId, cancellationToken);
            TempData["StatusMessage"] = "Password reset request closed.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not close password reset request: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SupportThreads(CancellationToken cancellationToken)
    {
        try
        {
            var threads = await messagingApiService.ListSupportThreadsAsync(cancellationToken);
            return View(new AdminSupportThreadsViewModel { Threads = threads });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not load support threads: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRestaurantAdmin(Guid restaurantId, string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            TempData["StatusMessage"] = "Username is required.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await restaurantApiService.GrantAdminAssignmentAsync(
                restaurantId,
                new CreateRestaurantAdminAssignmentRequest { Username = username.Trim() },
                cancellationToken);
            TempData["StatusMessage"] = "Restaurant admin assignment saved.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = ex.StatusCode == HttpStatusCode.NotFound
                ? "Restaurant operations are not enabled."
                : $"Assignment failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RevokeRestaurantAdmin(Guid restaurantId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await restaurantApiService.RevokeAdminAssignmentAsync(restaurantId, userId, cancellationToken);
            TempData["StatusMessage"] = "Restaurant admin assignment revoked.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = ex.StatusCode == HttpStatusCode.NotFound
                ? "Restaurant operations are not enabled."
                : $"Revoke failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminIndexViewModel> BuildIndexViewModelAsync(CancellationToken cancellationToken)
    {
        var reports = await moderationApiService.ListReportsAsync(
            new BrowseModerationReportsQuery { Status = ModerationReportStatus.Pending, PageSize = 50 },
            cancellationToken);
        var (restaurantOperationsAvailable, restaurantAssignments) = await BuildRestaurantAssignmentPanelAsync(cancellationToken);
        var openPasswordResetRequests = User.IsInRole("Admin")
            ? await authApiService.ListOpenPasswordResetRequestsAsync(cancellationToken)
            : [];

        return new AdminIndexViewModel
        {
            PendingReports = reports.Items,
            RestaurantOperationsAvailable = restaurantOperationsAvailable,
            RestaurantAssignments = restaurantAssignments,
            OpenPasswordResetRequests = openPasswordResetRequests,
        };
    }

    private async Task<(bool Available, IReadOnlyCollection<RestaurantAssignmentPanelItem> Items)> BuildRestaurantAssignmentPanelAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin"))
        {
            return (false, []);
        }

        var restaurants = await restaurantApiService.BrowseAsync(new BrowseRestaurantsQuery { PageSize = 2000 }, cancellationToken);
        var items = new List<RestaurantAssignmentPanelItem>(restaurants.Items.Count);

        foreach (var restaurant in restaurants.Items)
        {
            IReadOnlyCollection<RestaurantAdminAssignmentDto> assignments;

            try
            {
                assignments = await restaurantApiService.ListAdminAssignmentsAsync(restaurant.RestaurantId, cancellationToken);
            }
            catch (BackendApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return (false, []);
            }

            items.Add(new RestaurantAssignmentPanelItem
            {
                Restaurant = restaurant,
                Assignments = assignments,
            });
        }

        return (true, items);
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
