using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over profile-related backend routes used by the current MVC pages.
/// The methods deliberately mirror the backend endpoints to keep controller code obvious.
/// Register this class in Program.cs, then inject it into controllers that need profile data.
/// </summary>
public sealed class ProfileApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public ProfileApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    /// <summary>
    /// Reads the backend-owned onboarding status for the current user.
    /// </summary>
    public Task<OnboardingStatusDto> GetOnboardingStatusAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<OnboardingStatusDto>("/api/v1/onboarding/status", cancellationToken);

    /// <summary>
    /// Reads the current user's profile details from the backend.
    /// </summary>
    public Task<ProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ProfileDto>("/api/v1/profiles/me", cancellationToken);

    /// <summary>
    /// Reads the current user's saved food preferences from the backend.
    /// </summary>
    public Task<PreferenceDto> GetMyPreferencesAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<PreferenceDto>("/api/v1/preferences/me", cancellationToken);

    /// <summary>
    /// Reads the current user's privacy settings from the backend.
    /// </summary>
    public Task<PrivacySettingsDto> GetMyPrivacySettingsAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<PrivacySettingsDto>("/api/v1/privacy-settings/me", cancellationToken);

    /// <summary>
    /// Reads the dashboard summary data for the authenticated user.
    /// </summary>
    public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<DashboardDto>("/api/v1/me/dashboard", cancellationToken);

    /// <summary>
    /// Sends the edited profile section back to the backend.
    /// </summary>
    public Task<ProfileDto> UpdateMyProfileAsync(
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdateMyProfileRequest, ProfileDto>(
            "/api/v1/profiles/me",
            request,
            cancellationToken);

    /// <summary>
    /// Sends the edited preferences section back to the backend.
    /// </summary>
    public Task<PreferenceDto> ReplaceMyPreferencesAsync(
        ReplacePreferencesRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PutAsync<ReplacePreferencesRequest, PreferenceDto>(
            "/api/v1/preferences/me",
            request,
            cancellationToken);

    /// <summary>
    /// Sends the edited privacy section back to the backend.
    /// </summary>
    public Task<PrivacySettingsDto> UpdateMyPrivacySettingsAsync(
        UpdatePrivacySettingsRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdatePrivacySettingsRequest, PrivacySettingsDto>(
            "/api/v1/privacy-settings/me",
            request,
            cancellationToken);
}
