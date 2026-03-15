using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Web.Mvc.Services.Api;
using TasteBudz.Web.Mvc.Services.Http;
using TasteBudz.Web.Mvc.Services.Session;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

[Authorize]
/// <summary>
/// Handles the authenticated profile area.
/// The controller composes backend DTOs into page models but does not own business rules.
/// </summary>
public sealed class ProfileController : Controller
{
    private readonly ProfileApiService profileApiService;
    private readonly UserSessionService userSessionService;

    public ProfileController(
        ProfileApiService profileApiService,
        UserSessionService userSessionService)
    {
        this.profileApiService = profileApiService;
        this.userSessionService = userSessionService;
    }

    [HttpGet]
    public async Task<IActionResult> View(CancellationToken cancellationToken)
    {
        try
        {
            var onboardingStatus = await profileApiService.GetOnboardingStatusAsync(cancellationToken);

            if (!onboardingStatus.IsComplete)
            {
                return RedirectToAction(nameof(Edit));
            }

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
            // The edit page combines several backend endpoints into one MVC form model.
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
            // Each section of the page is still backed by its own backend endpoint.
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
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
