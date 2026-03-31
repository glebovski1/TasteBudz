using TasteBudz.Backend.Modules.Discovery;
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

    public Task<OnboardingStatusDto> GetOnboardingStatusAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<OnboardingStatusDto>("/api/v1/onboarding/status", cancellationToken);

    public Task<ProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ProfileDto>("/api/v1/profiles/me", cancellationToken);

    /// <summary>
    /// Fetches any user's public profile by ID. Used by the user pop-up card.
    /// </summary>
    public Task<ProfileDto> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ProfileDto>($"/api/v1/profiles/{userId}", cancellationToken);

    /// <summary>
    /// Returns true if the given user is a current Bud of the authenticated user.
    /// </summary>
    public async Task<bool> IsBudAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var budz = await ListBudzAsync(cancellationToken);
        return budz.Any(b => b.UserId == userId);
    }

    /// <summary>
    /// Fetches the authenticated user's Budz list.
    /// </summary>
    public Task<IReadOnlyCollection<BudConnectionDto>> ListBudzAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<BudConnectionDto>>("/api/v1/budz", cancellationToken);

    /// <summary>
    /// Removes a mutual Bud connection.
    /// </summary>
    public Task RemoveBudAsync(Guid otherUserId, CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync($"/api/v1/budz/{otherUserId}", cancellationToken: cancellationToken);

    public Task<PreferenceDto> GetMyPreferencesAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<PreferenceDto>("/api/v1/preferences/me", cancellationToken);

    public Task<PrivacySettingsDto> GetMyPrivacySettingsAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<PrivacySettingsDto>("/api/v1/privacy-settings/me", cancellationToken);

    public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<DashboardDto>("/api/v1/me/dashboard", cancellationToken);

    public Task<IReadOnlyCollection<DashboardEventSummaryDto>> ListMyEventsAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<DashboardEventSummaryDto>>("/api/v1/me/events", cancellationToken);

    public Task<IReadOnlyCollection<DashboardGroupSummaryDto>> ListMyGroupsAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<DashboardGroupSummaryDto>>("/api/v1/me/groups", cancellationToken);

    public Task<IReadOnlyCollection<EventInviteDto>> ListMyEventInvitesAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<EventInviteDto>>("/api/v1/me/event-invites", cancellationToken);

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

    public Task<IReadOnlyCollection<RecurringAvailabilityWindowDto>> ListRecurringAvailabilityAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<RecurringAvailabilityWindowDto>>("/api/v1/availability/recurring", cancellationToken);

    public Task<RecurringAvailabilityWindowDto> CreateRecurringAvailabilityAsync(
        UpsertRecurringAvailabilityWindowRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<UpsertRecurringAvailabilityWindowRequest, RecurringAvailabilityWindowDto>(
            "/api/v1/availability/recurring",
            request,
            cancellationToken: cancellationToken);

    public Task<RecurringAvailabilityWindowDto> UpdateRecurringAvailabilityAsync(
        Guid windowId,
        UpsertRecurringAvailabilityWindowRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpsertRecurringAvailabilityWindowRequest, RecurringAvailabilityWindowDto>(
            $"/api/v1/availability/recurring/{windowId}",
            request,
            cancellationToken);

    public Task DeleteRecurringAvailabilityAsync(Guid windowId, CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync($"/api/v1/availability/recurring/{windowId}", cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<OneOffAvailabilityWindowDto>> ListOneOffAvailabilityAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<OneOffAvailabilityWindowDto>>("/api/v1/availability/one-off", cancellationToken);

    public Task<OneOffAvailabilityWindowDto> CreateOneOffAvailabilityAsync(
        UpsertOneOffAvailabilityWindowRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<UpsertOneOffAvailabilityWindowRequest, OneOffAvailabilityWindowDto>(
            "/api/v1/availability/one-off",
            request,
            cancellationToken: cancellationToken);

    public Task<OneOffAvailabilityWindowDto> UpdateOneOffAvailabilityAsync(
        Guid windowId,
        UpsertOneOffAvailabilityWindowRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpsertOneOffAvailabilityWindowRequest, OneOffAvailabilityWindowDto>(
            $"/api/v1/availability/one-off/{windowId}",
            request,
            cancellationToken);

    public Task DeleteOneOffAvailabilityAsync(Guid windowId, CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync($"/api/v1/availability/one-off/{windowId}", cancellationToken: cancellationToken);

    public Task<PrivacySettingsDto> UpdateMyPrivacySettingsAsync(
        UpdatePrivacySettingsRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdatePrivacySettingsRequest, PrivacySettingsDto>(
            "/api/v1/privacy-settings/me",
            request,
            cancellationToken);

    public Task<IReadOnlyCollection<BlockedUserDto>> ListBlocksAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<BlockedUserDto>>("/api/v1/blocks", cancellationToken);

    public Task<BlockedUserDto> CreateBlockAsync(
        CreateBlockRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreateBlockRequest, BlockedUserDto>(
            "/api/v1/blocks",
            request,
            cancellationToken: cancellationToken);

    public Task RemoveBlockAsync(Guid blockedUserId, CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync($"/api/v1/blocks/{blockedUserId}", cancellationToken: cancellationToken);

    public Task RequestAccountDeletionAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync("/api/v1/account/deletion", cancellationToken: cancellationToken);
}