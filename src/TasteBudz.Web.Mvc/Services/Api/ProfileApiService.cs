using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Web.Mvc.Services.Http;

namespace TasteBudz.Web.Mvc.Services.Api;

/// <summary>
/// Thin wrapper over profile-related backend routes used by the current MVC pages.
/// The methods deliberately mirror the backend endpoints to keep controller code obvious.
/// </summary>
public sealed class ProfileApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public ProfileApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    public Task<OnboardingStatusDto> GetOnboardingStatusAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<OnboardingStatusDto>("/api/v1/onboarding/status", cancellationToken);

    public Task<ProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ProfileDto>("/api/v1/profiles/me", cancellationToken);

    public Task<PreferenceDto> GetMyPreferencesAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<PreferenceDto>("/api/v1/preferences/me", cancellationToken);

    public Task<PrivacySettingsDto> GetMyPrivacySettingsAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<PrivacySettingsDto>("/api/v1/privacy-settings/me", cancellationToken);

    public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<DashboardDto>("/api/v1/me/dashboard", cancellationToken);

    public Task<ProfileDto> UpdateMyProfileAsync(
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdateMyProfileRequest, ProfileDto>(
            "/api/v1/profiles/me",
            request,
            cancellationToken);

    public Task<PreferenceDto> ReplaceMyPreferencesAsync(
        ReplacePreferencesRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PutAsync<ReplacePreferencesRequest, PreferenceDto>(
            "/api/v1/preferences/me",
            request,
            cancellationToken);

    public Task<PrivacySettingsDto> UpdateMyPrivacySettingsAsync(
        UpdatePrivacySettingsRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdatePrivacySettingsRequest, PrivacySettingsDto>(
            "/api/v1/privacy-settings/me",
            request,
            cancellationToken);
}
