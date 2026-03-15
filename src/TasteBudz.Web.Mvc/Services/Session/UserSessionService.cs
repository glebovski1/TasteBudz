using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Web.Mvc.Services.Http;

namespace TasteBudz.Web.Mvc.Services.Session;

/// <summary>
/// Bridges backend token-based auth and MVC cookie-based auth.
/// It stores the backend session in ASP.NET session and keeps the MVC auth cookie in sync.
/// </summary>
public sealed class UserSessionService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private const string SessionKey = "TasteBudz.BackendSession";

    public UserSessionService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public BackendSessionSnapshot? GetSession()
    {
        var httpContext = GetHttpContext();
        var json = httpContext.Session.GetString(SessionKey);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<BackendSessionSnapshot>(json, BackendJson.Options);
    }

    public BackendSessionSnapshot GetRequiredSession() =>
        GetSession() ?? throw new BackendAuthenticationExpiredException("The current session is no longer available.");

    public async Task SignInAsync(SessionDto session, CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();
        var snapshot = BackendSessionSnapshot.FromDto(session);
        // Save backend tokens first, then sign in the MVC cookie principal.
        SaveSnapshot(httpContext, snapshot);
        await SignInPrincipalAsync(httpContext, snapshot);
    }

    public async Task UpdateSessionAsync(SessionDto session, CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();
        var snapshot = BackendSessionSnapshot.FromDto(session);
        SaveSnapshot(httpContext, snapshot);
        await SignInPrincipalAsync(httpContext, snapshot);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();
        httpContext.Session.Remove(SessionKey);
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static void SaveSnapshot(HttpContext httpContext, BackendSessionSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, BackendJson.Options);
        httpContext.Session.SetString(SessionKey, json);
    }

    private static Task SignInPrincipalAsync(HttpContext httpContext, BackendSessionSnapshot snapshot)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, snapshot.UserId.ToString()),
            new(ClaimTypes.Name, snapshot.Username),
            new(ClaimTypes.Email, snapshot.Email),
        };

        claims.AddRange(snapshot.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));

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

public sealed record BackendSessionSnapshot(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId,
    string Username,
    string Email,
    IReadOnlyCollection<TasteBudz.Backend.Domain.UserRole> Roles)
{
    /// <summary>
    /// Flattens the backend session DTO into the small shape we keep in MVC session state.
    /// </summary>
    public static BackendSessionSnapshot FromDto(SessionDto session) =>
        new(
            session.AccessToken,
            session.RefreshToken,
            session.ExpiresAtUtc,
            session.CurrentUser.UserId,
            session.CurrentUser.Username,
            session.CurrentUser.Email,
            session.CurrentUser.Roles);
}
