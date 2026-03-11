using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.Services.Backend;

public sealed class OnboardingApiClient(BackendApiRequestExecutor executor)
{
    public Task<OnboardingStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
        executor.GetAsync<OnboardingStatusDto>("/api/v1/onboarding/status", cancellationToken);
}
