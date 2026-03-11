using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.Services.Session;

public interface IUserSessionService
{
    BackendSessionSnapshot? GetSnapshot();

    BackendSessionSnapshot GetRequiredSnapshot();

    Task SignInAsync(SessionDto session, CancellationToken cancellationToken = default);

    Task UpdateBackendSessionAsync(SessionDto session, CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}
