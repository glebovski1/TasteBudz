using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TasteBudz.Web.Mvc.Services.Backend;
using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.Services.Session;

public sealed class UserSessionService(IHttpContextAccessor httpContextAccessor) : IUserSessionService
{
    private const string SessionKey = "TasteBudz.BackendSession";

    public BackendSessionSnapshot? GetSnapshot()
    {
        var httpContext = GetHttpContext();
        var json = httpContext.Session.GetString(SessionKey);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<BackendSessionSnapshot>(json, BackendJson.Options);
    }

    public BackendSessionSnapshot GetRequiredSnapshot() =>
        GetSnapshot() ?? throw new BackendAuthenticationExpiredException("The current session is no longer available.");

    public async Task SignInAsync(SessionDto session, CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();
        var snapshot = CreateSnapshot(session);
        SaveSnapshot(httpContext, snapshot);
        await SignInPrincipalAsync(httpContext, snapshot, cancellationToken);
    }

    public async Task UpdateBackendSessionAsync(SessionDto session, CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();
        var snapshot = CreateSnapshot(session);
        SaveSnapshot(httpContext, snapshot);
        await SignInPrincipalAsync(httpContext, snapshot, cancellationToken);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();
        httpContext.Session.Remove(SessionKey);
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static BackendSessionSnapshot CreateSnapshot(SessionDto session) =>
        new(session.AccessToken, session.RefreshToken, session.ExpiresAtUtc, session.CurrentUser);

    private static void SaveSnapshot(HttpContext httpContext, BackendSessionSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, BackendJson.Options);
        httpContext.Session.SetString(SessionKey, json);
    }

    private static Task SignInPrincipalAsync(
        HttpContext httpContext,
        BackendSessionSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, snapshot.CurrentUser.UserId.ToString()),
            new(ClaimTypes.Name, snapshot.CurrentUser.Username),
            new(ClaimTypes.Email, snapshot.CurrentUser.Email),
        };

        claims.AddRange(snapshot.CurrentUser.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

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
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("The current request context is not available.");
}
