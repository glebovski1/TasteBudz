using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Bridges backend token-based auth and MVC cookie-based auth.
/// It stores the backend SessionDto directly in ASP.NET session and keeps the MVC auth cookie in sync.
/// Register this class in Program.cs, then inject it into controllers or other services that need
/// to read the current backend session, sign a user in, refresh stored tokens, or sign a user out.
/// </summary>
public sealed class UserSessionService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private const string SessionKey = "TasteBudz.BackendSession";

    public UserSessionService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Reads the current backend session from ASP.NET session storage.
    /// Returns null when the browser does not currently have a backend session.
    /// </summary>
    public SessionDto? GetSession()
    {
        var httpContext = GetHttpContext();
        var json = httpContext.Session.GetString(SessionKey);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<SessionDto>(json, BackendJson.Options);
    }

    /// <summary>
    /// Reads the current backend session and throws when it does not exist.
    /// BackendHttpClient uses this when a protected API call requires an access token.
    /// </summary>
    public SessionDto GetRequiredSession() =>
        GetSession() ?? throw new BackendAuthenticationExpiredException("The current session is no longer available.");

    /// <summary>
    /// Saves the backend session and signs in the local MVC auth cookie.
    /// This is used immediately after successful login or registration.
    /// </summary>
    public async Task SignInAsync(SessionDto session, CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();

        // Step 1:
        // Save the full backend SessionDto exactly as the backend returned it.
        SaveSession(httpContext, session);

        // Step 2:
        // Build the MVC cookie principal from the backend user data so [Authorize] works in MVC.
        await SignInPrincipalAsync(httpContext, session);
    }

    /// <summary>
    /// Replaces the stored backend session and updates the MVC cookie principal.
    /// BackendHttpClient calls this after a successful token refresh.
    /// </summary>
    public async Task UpdateSessionAsync(SessionDto session, CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();

        // Save the refreshed tokens first.
        SaveSession(httpContext, session);

        // Then rebuild the cookie principal so local MVC auth stays aligned with backend user data.
        await SignInPrincipalAsync(httpContext, session);
    }

    /// <summary>
    /// Clears both local MVC auth mechanisms:
    /// the ASP.NET session entry that stores backend tokens and the MVC auth cookie.
    /// </summary>
    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();
        httpContext.Session.Remove(SessionKey);
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static void SaveSession(HttpContext httpContext, SessionDto session)
    {
        // SessionDto is stored directly to avoid introducing one more MVC-only session shape.
        var json = JsonSerializer.Serialize(session, BackendJson.Options);
        httpContext.Session.SetString(SessionKey, json);
    }

    private static Task SignInPrincipalAsync(HttpContext httpContext, SessionDto session)
    {
        // These claims are the local MVC identity projection of the backend current-user summary.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.CurrentUser.UserId.ToString()),
            new(ClaimTypes.Name, session.CurrentUser.Username),
            new(ClaimTypes.Email, session.CurrentUser.Email),
        };

        // Copy backend roles into MVC role claims so standard authorization checks continue to work.
        claims.AddRange(session.CurrentUser.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        // SignInAsync writes the encrypted auth cookie for the browser.
        return httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = false,
            });
    }

    private HttpContext GetHttpContext() =>
        // All session and cookie operations need the active request HttpContext.
        // Throwing here makes missing request context failures obvious.
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("The current request context is not available.");
}
