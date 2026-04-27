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

    [HttpGet("Admin/Reports/{id:guid}")]
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

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restaurants(
        string? q,
        AdminRestaurantCatalogStatus? status,
        AdminRestaurantCatalogSource? source,
        int page = 1,
        Guid? editRestaurantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return View(await BuildRestaurantCatalogViewModelAsync(q, status, source, page, editRestaurantId, importPreview: null, importForm: null, cancellationToken));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
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

        return RedirectToAction(nameof(Restaurants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PreviewRestaurantImport(RestaurantImportPreviewForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Import geography is invalid.";
            return RedirectToAction(nameof(Restaurants));
        }

        try
        {
            var preview = await restaurantApiService.PreviewImportFromOverpassAsync(form.ToQuery(), cancellationToken);
            TempData["StatusMessage"] = $"Preview found {preview.CandidateCount} candidates, {preview.ImportableCount} importable.";
            return View("Restaurants", await BuildRestaurantCatalogViewModelAsync(null, null, null, 1, null, preview, form, cancellationToken));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Import preview failed: {ex.Message}";
            return RedirectToAction(nameof(Restaurants));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CommitRestaurantImport(RestaurantImportCommitForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Import selection is invalid.";
            return RedirectToAction(nameof(Restaurants));
        }

        try
        {
            var result = await restaurantApiService.CommitImportFromOverpassAsync(form.ToRequest(), cancellationToken);
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

        return RedirectToAction(nameof(Restaurants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRestaurant(AdminRestaurantCatalogForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Restaurant details are invalid.";
            return RedirectToAction(nameof(Restaurants));
        }

        try
        {
            await restaurantApiService.CreateAdminRestaurantAsync(form.ToRequest(), cancellationToken);
            TempData["StatusMessage"] = "Restaurant created.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Restaurant creation failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Restaurants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCatalogRestaurant(AdminRestaurantCatalogForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || form.RestaurantId == Guid.Empty)
        {
            TempData["StatusMessage"] = "Restaurant details are invalid.";
            return RedirectToAction(nameof(Restaurants));
        }

        try
        {
            await restaurantApiService.UpdateAdminRestaurantAsync(form.RestaurantId, form.ToRequest(), cancellationToken);
            TempData["StatusMessage"] = "Restaurant updated.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Restaurant update failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Restaurants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ArchiveRestaurant(Guid restaurantId, CancellationToken cancellationToken)
    {
        try
        {
            await restaurantApiService.ArchiveAdminRestaurantAsync(restaurantId, cancellationToken);
            TempData["StatusMessage"] = "Restaurant archived.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Restaurant archive failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Restaurants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestoreRestaurant(Guid restaurantId, CancellationToken cancellationToken)
    {
        try
        {
            await restaurantApiService.RestoreAdminRestaurantAsync(restaurantId, cancellationToken);
            TempData["StatusMessage"] = "Restaurant restored.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Restaurant restore failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Restaurants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRestaurantAdmin(Guid restaurantId, string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            TempData["StatusMessage"] = "Username is required.";
            return RedirectToAction(nameof(Restaurants));
        }

        try
        {
            await restaurantApiService.GrantAdminAssignmentAsync(
                restaurantId,
                new CreateRestaurantAdminAssignmentRequest { Username = username.Trim() },
                cancellationToken);
            TempData["StatusMessage"] = "Restaurant admin assignment saved. The assigned user should sign out and back in to refresh access.";
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

        return RedirectToAction(nameof(Restaurants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RevokeRestaurantAdmin(Guid restaurantId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await restaurantApiService.RevokeAdminAssignmentAsync(restaurantId, userId, cancellationToken);
            TempData["StatusMessage"] = "Restaurant admin assignment revoked. The user should sign out and back in to refresh access.";
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

        return RedirectToAction(nameof(Restaurants));
    }

    private async Task<AdminIndexViewModel> BuildIndexViewModelAsync(CancellationToken cancellationToken)
    {
        var reports = await moderationApiService.ListReportsAsync(
            new BrowseModerationReportsQuery { Status = ModerationReportStatus.Pending, PageSize = 50 },
            cancellationToken);
        var (restaurantOperationsAvailable, restaurantCatalogTotalCount) = await BuildRestaurantSummaryAsync(cancellationToken);
        var openPasswordResetRequests = User.IsInRole("Admin")
            ? await authApiService.ListOpenPasswordResetRequestsAsync(cancellationToken)
            : [];

        return new AdminIndexViewModel
        {
            PendingReports = reports.Items,
            RestaurantOperationsAvailable = restaurantOperationsAvailable,
            RestaurantAssignments = [],
            RestaurantCatalogTotalCount = restaurantCatalogTotalCount,
            OpenPasswordResetRequests = openPasswordResetRequests,
        };
    }

    private async Task<(bool Available, int TotalCount)> BuildRestaurantSummaryAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin"))
        {
            return (false, 0);
        }

        try
        {
            var restaurants = await restaurantApiService.SearchAdminRestaurantsAsync(
                new AdminRestaurantSearchQuery { Page = 1, PageSize = 1, Status = AdminRestaurantCatalogStatus.All },
                cancellationToken);
            return (true, restaurants.TotalCount);
        }
        catch (BackendApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return (false, 0);
        }
    }

    private async Task<AdminRestaurantsViewModel> BuildRestaurantCatalogViewModelAsync(
        string? q,
        AdminRestaurantCatalogStatus? status,
        AdminRestaurantCatalogSource? source,
        int page,
        Guid? editRestaurantId,
        RestaurantImportPreviewDto? importPreview,
        RestaurantImportPreviewForm? importForm,
        CancellationToken cancellationToken)
    {
        var currentPage = Math.Max(1, page);
        var restaurants = await restaurantApiService.SearchAdminRestaurantsAsync(
            new AdminRestaurantSearchQuery
            {
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                Status = status ?? AdminRestaurantCatalogStatus.All,
                Source = source ?? AdminRestaurantCatalogSource.All,
                Page = currentPage,
                PageSize = AdminRestaurantsViewModel.PageSize,
            },
            cancellationToken);
        var assignmentsByRestaurantId = new Dictionary<Guid, IReadOnlyCollection<RestaurantAdminAssignmentDto>>();

        foreach (var restaurant in restaurants.Items)
        {
            try
            {
                assignmentsByRestaurantId[restaurant.RestaurantId] =
                    await restaurantApiService.ListAdminAssignmentsAsync(restaurant.RestaurantId, cancellationToken);
            }
            catch (BackendApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                assignmentsByRestaurantId[restaurant.RestaurantId] = [];
            }
        }

        var suggestedCuisineTags = CuisineData.AvailableCuisineTags
            .Append("Other")
            .Concat(restaurants.Items.SelectMany(item => item.CuisineTags))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AdminRestaurantsViewModel
        {
            Restaurants = restaurants.Items.Select(restaurant => new AdminRestaurantCatalogItemViewModel
            {
                Restaurant = restaurant,
                Form = AdminRestaurantCatalogForm.FromDto(restaurant),
            }).ToArray(),
            AssignmentsByRestaurantId = assignmentsByRestaurantId,
            SuggestedCuisineTags = suggestedCuisineTags,
            Q = q,
            FilterStatus = status ?? AdminRestaurantCatalogStatus.All,
            FilterSource = source ?? AdminRestaurantCatalogSource.All,
            CurrentPage = currentPage,
            TotalCount = restaurants.TotalCount,
            EditRestaurantId = editRestaurantId,
            ImportPreview = importPreview,
            ImportForm = importForm ?? new RestaurantImportPreviewForm(),
        };
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
