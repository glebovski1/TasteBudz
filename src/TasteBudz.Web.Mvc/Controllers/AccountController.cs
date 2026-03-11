using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Web.Mvc.Services.Backend;
using TasteBudz.Web.Mvc.Services.Backend.Contracts;
using TasteBudz.Web.Mvc.Services.Session;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

public sealed class AccountController(
    AuthApiClient authApiClient,
    OnboardingApiClient onboardingApiClient,
    IUserSessionService userSessionService) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(ProfileController.View), "Profile");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var session = await authApiClient.LoginAsync(
                new LoginRequest
                {
                    UsernameOrEmail = model.UsernameOrEmail.Trim(),
                    Password = model.Password,
                },
                cancellationToken);

            await userSessionService.SignInAsync(session, cancellationToken);
            return await RedirectAfterAuthenticationAsync(cancellationToken);
        }
        catch (BackendApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult CreateAccount()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(ProfileController.View), "Profile");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var session = await authApiClient.RegisterAsync(
                new RegisterUserRequest
                {
                    Username = model.Username.Trim(),
                    Email = model.Email.Trim(),
                    Password = model.Password,
                    ZipCode = model.ZipCode.Trim(),
                },
                cancellationToken);

            await userSessionService.SignInAsync(session, cancellationToken);
            return await RedirectAfterAuthenticationAsync(cancellationToken);
        }
        catch (BackendApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var snapshot = userSessionService.GetSnapshot();

        if (snapshot is not null)
        {
            try
            {
                await authApiClient.LogoutAsync(snapshot.AccessToken, cancellationToken);
            }
            catch (BackendApiException)
            {
                // Clearing the local auth/session is still correct even if the backend token is already expired.
            }
        }

        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(Login));
    }

    private async Task<IActionResult> RedirectAfterAuthenticationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var onboardingStatus = await onboardingApiClient.GetStatusAsync(cancellationToken);
            return onboardingStatus.IsComplete
                ? RedirectToAction(nameof(ProfileController.View), "Profile")
                : RedirectToAction(nameof(ProfileController.Edit), "Profile");
        }
        catch (BackendAuthenticationExpiredException)
        {
            await userSessionService.SignOutAsync(cancellationToken);
            ModelState.AddModelError(string.Empty, "Your session expired before the app could finish signing you in. Please try again.");
            return View("Login", new LoginViewModel());
        }
    }
}
