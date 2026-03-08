// Business rules for reading and replacing user food preferences.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Time;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Normalizes preference lists before persistence so browse and recommendation logic sees stable values.
/// </summary>
public sealed class PreferenceService(IProfileRepository profileRepository, Infrastructure.Time.IClock clock)
{
    public async Task<PreferenceDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await profileRepository.GetPreferencesAsync(userId, cancellationToken)
            ?? new UserPreferences(userId, Array.Empty<string>(), null, Array.Empty<string>(), Array.Empty<string>(), clock.UtcNow);

        return ToDto(preferences);
    }

    public async Task<PreferenceDto> ReplaceAsync(Guid userId, ReplacePreferencesRequest request, CancellationToken cancellationToken = default)
    {
        var updated = new UserPreferences(
            userId,
            NormalizeList(request.CuisineTags),
            request.SpiceTolerance,
            NormalizeList(request.DietaryFlags),
            NormalizeList(request.Allergies),
            clock.UtcNow);

        await profileRepository.SavePreferencesAsync(updated, cancellationToken);
        return ToDto(updated);
    }

    private static PreferenceDto ToDto(UserPreferences preferences) =>
        new(preferences.CuisineTags, preferences.SpiceTolerance, preferences.DietaryFlags, preferences.Allergies);

    private static IReadOnlyCollection<string> NormalizeList(IReadOnlyCollection<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}