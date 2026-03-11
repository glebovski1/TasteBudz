using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.Services.Backend;

public sealed class PrivacyApiClient(BackendApiRequestExecutor executor)
{
    public Task<PrivacySettingsDto> GetMyPrivacySettingsAsync(CancellationToken cancellationToken = default) =>
        executor.GetAsync<PrivacySettingsDto>("/api/v1/privacy-settings/me", cancellationToken);

    public Task<PrivacySettingsDto> UpdateMyPrivacySettingsAsync(UpdatePrivacySettingsRequest request, CancellationToken cancellationToken = default) =>
        executor.PatchAsync<UpdatePrivacySettingsRequest, PrivacySettingsDto>("/api/v1/privacy-settings/me", request, cancellationToken);
}
