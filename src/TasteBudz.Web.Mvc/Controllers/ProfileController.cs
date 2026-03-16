using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Handles the authenticated profile area.
/// The controller composes backend DTOs into page models but does not own business rules.
/// </summary>
[Authorize]
public sealed class ProfileController : Controller
{
    private readonly ProfileApiService profileApiService;
    private readonly UserSessionService userSessionService;

    public ProfileController(
        ProfileApiService profileApiService,
        UserSessionService userSessionService)
    {
        // ASP.NET Core DI creates this controller and supplies these concrete services automatically.
        // Program.cs registers both services, so the controller only needs to ask for them here.
        this.profileApiService = profileApiService;
        this.userSessionService = userSessionService;
    }

    [HttpGet]
    public async Task<IActionResult> View(CancellationToken cancellationToken)
    {
        try
        {
            // Step 1:
            // Confirm onboarding status before showing the dashboard.
            var onboardingStatus = await profileApiService.GetOnboardingStatusAsync(cancellationToken);

            if (!onboardingStatus.IsComplete)
            {
                return RedirectToAction(nameof(Edit));
            }

            // Step 2:
            // Read dashboard data from the backend and map it to the Razor page view model.
            var dashboard = await profileApiService.GetDashboardAsync(cancellationToken);
            return View(DashboardViewModel.FromDto(dashboard));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        try
        {
            // The edit page needs data from multiple backend endpoints.
            // The controller gathers each DTO and combines them into one MVC form model.
            var onboardingStatus = await profileApiService.GetOnboardingStatusAsync(cancellationToken);
            var profile = await profileApiService.GetMyProfileAsync(cancellationToken);
            var preferences = await profileApiService.GetMyPreferencesAsync(cancellationToken);
            var privacySettings = await profileApiService.GetMyPrivacySettingsAsync(cancellationToken);

            return View(ProfileEditViewModel.FromDto(profile, preferences, privacySettings, onboardingStatus));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model, CancellationToken cancellationToken)
    {
        model.NormalizeSelections();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Save each section through its own backend endpoint.
            // The backend remains the source of truth for profile, preferences, and privacy rules.
            await profileApiService.UpdateMyProfileAsync(model.ToProfileRequest(), cancellationToken);
            await profileApiService.ReplaceMyPreferencesAsync(model.ToPreferenceRequest(), cancellationToken);
            await profileApiService.UpdateMyPrivacySettingsAsync(model.ToPrivacyRequest(), cancellationToken);

            TempData["StatusMessage"] = "Profile saved.";
            return RedirectToAction(nameof(View));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        // If backend auth expired, clear local auth too, then send the user back to login.
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
