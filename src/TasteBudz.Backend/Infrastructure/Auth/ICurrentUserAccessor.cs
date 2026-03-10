// Abstraction used by controllers to obtain the authenticated TasteBudz user.
namespace TasteBudz.Backend.Infrastructure.Auth;

/// <summary>
/// Exposes the authenticated caller in application terms instead of raw ASP.NET Core claims APIs.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Returns the current user or throws when the request is anonymous.
    /// </summary>
    CurrentUser GetRequiredCurrentUser();

    /// <summary>
    /// Returns the current user when available, otherwise <c>null</c>.
    /// </summary>
    CurrentUser? TryGetCurrentUser();
}