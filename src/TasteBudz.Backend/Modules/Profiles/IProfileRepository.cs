// Persistence boundary for profile, preference, availability, privacy, and block data.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Stores the user-owned profile data used across onboarding, discovery, and events.
/// </summary>
public interface IProfileRepository
{
    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveProfileAsync(UserProfile profile, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserProfile>> ListProfilesAsync(CancellationToken cancellationToken = default);

    Task<UserPreferences?> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RecurringAvailabilityWindow>> ListRecurringAvailabilityAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OneOffAvailabilityWindow>> ListOneOffAvailabilityAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<RecurringAvailabilityWindow?> GetRecurringAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default);

    Task<OneOffAvailabilityWindow?> GetOneOffAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default);

    Task SaveRecurringAvailabilityAsync(RecurringAvailabilityWindow window, CancellationToken cancellationToken = default);

    Task SaveOneOffAvailabilityAsync(OneOffAvailabilityWindow window, CancellationToken cancellationToken = default);

    Task DeleteRecurringAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default);

    Task DeleteOneOffAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default);

    Task<PrivacySettings?> GetPrivacySettingsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SavePrivacySettingsAsync(PrivacySettings settings, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserBlock>> ListBlocksAsync(Guid blockerUserId, CancellationToken cancellationToken = default);

    Task<UserBlock?> GetBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default);

    Task SaveBlockAsync(UserBlock block, CancellationToken cancellationToken = default);

    Task DeleteBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default);
}