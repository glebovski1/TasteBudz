using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Web.Mvc.Services.Api;
using TasteBudz.Web.Mvc.Services.Http;
using TasteBudz.Web.Mvc.Services.Session;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Handles account entry points such as login, registration, and logout.
/// The controller owns page flow while the API services own backend communication.
/// </summary>
public sealed class AccountController : Controller
{
    private readonly AuthApiService authApiService;
    private readonly ProfileApiService profileApiService;
    private readonly UserSessionService userSessionService;

    public AccountController(
        AuthApiService authApiService,
        ProfileApiService profileApiService,
        UserSessionService userSessionService)
    {
        this.authApiService = authApiService;
        this.profileApiService = profileApiService;
        this.userSessionService = userSessionService;
    }

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
            // Login is a backend concern; the MVC app only forwards the form values.
            var session = await authApiService.LoginAsync(
                new LoginRequest
                {
                    UsernameOrEmail = model.UsernameOrEmail.Trim(),
                    Password = model.Password,
                },
                cancellationToken);

            // Once the backend returns a session, store it locally and sign in the MVC cookie.
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
            // Registration returns the same backend session payload as login.
            var session = await authApiService.RegisterAsync(
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
        var session = userSessionService.GetSession();

        if (session is not null)
        {
            try
            {
                await authApiService.LogoutAsync(cancellationToken);
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
            // The backend decides whether onboarding is complete; MVC only redirects accordingly.
            var onboardingStatus = await profileApiService.GetOnboardingStatusAsync(cancellationToken);
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
