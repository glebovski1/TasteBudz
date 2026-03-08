// Business rules for profile privacy settings.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Time;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Reads and updates privacy settings that affect discovery visibility.
/// </summary>
public sealed class PrivacyService(IProfileRepository profileRepository, IClock clock)
{
    public async Task<PrivacySettingsDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await profileRepository.GetPrivacySettingsAsync(userId, cancellationToken)
            ?? new PrivacySettings(userId, true, clock.UtcNow);

        return new PrivacySettingsDto(settings.DiscoveryEnabled);
    }

    public async Task<PrivacySettingsDto> UpdateAsync(Guid userId, UpdatePrivacySettingsRequest request, CancellationToken cancellationToken = default)
    {
        var settings = new PrivacySettings(userId, request.DiscoveryEnabled ?? true, clock.UtcNow);
        await profileRepository.SavePrivacySettingsAsync(settings, cancellationToken);
        return new PrivacySettingsDto(settings.DiscoveryEnabled);
    }
}