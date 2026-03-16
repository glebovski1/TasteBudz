using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Keeps the MVC auth cookie aligned with the backend session stored in ASP.NET session state.
/// If the backend session entry is gone, the browser should no longer be treated as authenticated.
/// </summary>
public sealed class BackendSessionCookieEvents : CookieAuthenticationEvents
{
    private readonly UserSessionService userSessionService;

    public BackendSessionCookieEvents(UserSessionService userSessionService)
    {
        this.userSessionService = userSessionService;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        try
        {
            var session = userSessionService.GetSession();
            var cookieUserId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionUserId = session?.CurrentUser.UserId.ToString();

            if (session is not null &&
                !string.IsNullOrWhiteSpace(session.RefreshToken) &&
                string.Equals(cookieUserId, sessionUserId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
        catch (JsonException)
        {
            // Invalid session payloads should be treated the same as a missing session entry.
        }

        context.RejectPrincipal();
        await userSessionService.SignOutAsync(context.HttpContext.RequestAborted);
    }
}
