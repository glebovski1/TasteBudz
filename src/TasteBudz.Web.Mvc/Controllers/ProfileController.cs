using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Web.Mvc.Services.Backend;
using TasteBudz.Web.Mvc.Services.Session;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

[Authorize]
public sealed class ProfileController(
    DashboardApiClient dashboardApiClient,
    OnboardingApiClient onboardingApiClient,
    PreferenceApiClient preferenceApiClient,
    PrivacyApiClient privacyApiClient,
    ProfileApiClient profileApiClient,
    IUserSessionService userSessionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> View(CancellationToken cancellationToken)
    {
        try
        {
            var onboardingStatus = await onboardingApiClient.GetStatusAsync(cancellationToken);

            if (!onboardingStatus.IsComplete)
            {
                return RedirectToAction(nameof(Edit));
            }

            var dashboard = await dashboardApiClient.GetDashboardAsync(cancellationToken);
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
            var onboardingStatus = await onboardingApiClient.GetStatusAsync(cancellationToken);
            var profile = await profileApiClient.GetMyProfileAsync(cancellationToken);
            var preferences = await preferenceApiClient.GetMyPreferencesAsync(cancellationToken);
            var privacySettings = await privacyApiClient.GetMyPrivacySettingsAsync(cancellationToken);

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
            await profileApiClient.UpdateMyProfileAsync(model.ToProfileRequest(), cancellationToken);
            await preferenceApiClient.ReplaceMyPreferencesAsync(model.ToPreferenceRequest(), cancellationToken);
            await privacyApiClient.UpdateMyPrivacySettingsAsync(model.ToPrivacyRequest(), cancellationToken);

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
