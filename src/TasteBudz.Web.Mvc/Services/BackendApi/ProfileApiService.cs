using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over the current-user backend routes used by the MVC app.
/// The methods deliberately mirror the backend endpoints to keep controller code obvious.
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
    /// Reads the authenticated user's active event summaries from the backend.
    /// </summary>
    public Task<IReadOnlyCollection<DashboardEventSummaryDto>> ListMyEventsAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<DashboardEventSummaryDto>>("/api/v1/me/events", cancellationToken);

    /// <summary>
    /// Reads the authenticated user's active group summaries from the backend.
    /// </summary>
    public Task<IReadOnlyCollection<DashboardGroupSummaryDto>> ListMyGroupsAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<DashboardGroupSummaryDto>>("/api/v1/me/groups", cancellationToken);

    /// <summary>
    /// Reads the authenticated user's pending event invites from the backend.
    /// </summary>
    public Task<IReadOnlyCollection<EventInviteDto>> ListMyEventInvitesAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<EventInviteDto>>("/api/v1/me/event-invites", cancellationToken);

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
    /// Reads the current user's recurring weekly availability windows.
    /// </summary>
    public Task<IReadOnlyCollection<RecurringAvailabilityWindowDto>> ListRecurringAvailabilityAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<RecurringAvailabilityWindowDto>>("/api/v1/availability/recurring", cancellationToken);

    /// <summary>
    /// Creates one recurring weekly availability window.
    /// </summary>
    public Task<RecurringAvailabilityWindowDto> CreateRecurringAvailabilityAsync(
        UpsertRecurringAvailabilityWindowRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<UpsertRecurringAvailabilityWindowRequest, RecurringAvailabilityWindowDto>(
            "/api/v1/availability/recurring",
            request,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Updates one recurring weekly availability window.
    /// </summary>
    public Task<RecurringAvailabilityWindowDto> UpdateRecurringAvailabilityAsync(
        Guid windowId,
        UpsertRecurringAvailabilityWindowRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpsertRecurringAvailabilityWindowRequest, RecurringAvailabilityWindowDto>(
            $"/api/v1/availability/recurring/{windowId}",
            request,
            cancellationToken);

    /// <summary>
    /// Deletes one recurring weekly availability window.
    /// </summary>
    public Task DeleteRecurringAvailabilityAsync(Guid windowId, CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync($"/api/v1/availability/recurring/{windowId}", cancellationToken: cancellationToken);

    /// <summary>
    /// Reads the current user's one-off availability windows.
    /// </summary>
    public Task<IReadOnlyCollection<OneOffAvailabilityWindowDto>> ListOneOffAvailabilityAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<OneOffAvailabilityWindowDto>>("/api/v1/availability/one-off", cancellationToken);

    /// <summary>
    /// Creates one one-off availability window.
    /// </summary>
    public Task<OneOffAvailabilityWindowDto> CreateOneOffAvailabilityAsync(
        UpsertOneOffAvailabilityWindowRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<UpsertOneOffAvailabilityWindowRequest, OneOffAvailabilityWindowDto>(
            "/api/v1/availability/one-off",
            request,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Updates one one-off availability window.
    /// </summary>
    public Task<OneOffAvailabilityWindowDto> UpdateOneOffAvailabilityAsync(
        Guid windowId,
        UpsertOneOffAvailabilityWindowRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpsertOneOffAvailabilityWindowRequest, OneOffAvailabilityWindowDto>(
            $"/api/v1/availability/one-off/{windowId}",
            request,
            cancellationToken);

    /// <summary>
    /// Deletes one one-off availability window.
    /// </summary>
    public Task DeleteOneOffAvailabilityAsync(Guid windowId, CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync($"/api/v1/availability/one-off/{windowId}", cancellationToken: cancellationToken);

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

    /// <summary>
    /// Reads the current user's block list.
    /// </summary>
    public Task<IReadOnlyCollection<BlockedUserDto>> ListBlocksAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<BlockedUserDto>>("/api/v1/blocks", cancellationToken);

    /// <summary>
    /// Creates a new user block.
    /// </summary>
    public Task<BlockedUserDto> CreateBlockAsync(
        CreateBlockRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreateBlockRequest, BlockedUserDto>(
            "/api/v1/blocks",
            request,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Removes one user block.
    /// </summary>
    public Task RemoveBlockAsync(Guid blockedUserId, CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync($"/api/v1/blocks/{blockedUserId}", cancellationToken: cancellationToken);

    /// <summary>
    /// Requests soft deletion of the authenticated user's account.
    /// </summary>
    public Task RequestAccountDeletionAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync("/api/v1/account/deletion", cancellationToken: cancellationToken);
}
