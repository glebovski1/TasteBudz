// Reads the authenticated principal from HttpContext and converts it into the app's CurrentUser model.
using System.Security.Claims;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.ProblemDetails;

namespace TasteBudz.Backend.Infrastructure.Auth;

/// <summary>
/// Provides a single place for converting ASP.NET Core claims into application-level caller data.
/// </summary>
public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    /// <summary>
    /// Returns the authenticated caller or raises a 401-style exception when no session is available.
    /// </summary>
    public CurrentUser GetRequiredCurrentUser() =>
        TryGetCurrentUser() ?? throw ApiException.Unauthorized();

    /// <summary>
    /// Best-effort principal conversion for endpoints that may allow anonymous access.
    /// </summary>
    public CurrentUser? TryGetCurrentUser()
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = principal.FindFirstValue(ClaimTypes.Name);

        if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        // Only recognized enum values are kept so malformed claims do not crash request processing.
        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => Enum.TryParse<UserRole>(claim.Value, true, out var role) ? (UserRole?)role : null)
            .Where(role => role.HasValue)
            .Select(role => role!.Value)
            .ToArray();

        return new CurrentUser(userId, username, roles);
    }
}