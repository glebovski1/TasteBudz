using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.Services.Backend;

public sealed class DashboardApiClient(BackendApiRequestExecutor executor)
{
    public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        executor.GetAsync<DashboardDto>("/api/v1/me/dashboard", cancellationToken);
}
