using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.Services.Backend;

public sealed class PreferenceApiClient(BackendApiRequestExecutor executor)
{
    public Task<PreferenceDto> GetMyPreferencesAsync(CancellationToken cancellationToken = default) =>
        executor.GetAsync<PreferenceDto>("/api/v1/preferences/me", cancellationToken);

    public Task<PreferenceDto> ReplaceMyPreferencesAsync(ReplacePreferencesRequest request, CancellationToken cancellationToken = default) =>
        executor.PutAsync<ReplacePreferencesRequest, PreferenceDto>("/api/v1/preferences/me", request, cancellationToken);
}
