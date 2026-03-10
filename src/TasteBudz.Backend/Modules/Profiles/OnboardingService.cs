// Determines whether the current user has completed the required onboarding inputs.
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Evaluates onboarding completeness from the persisted profile and preference records.
/// </summary>
public sealed class OnboardingService(IAuthRepository authRepository, IProfileRepository profileRepository)
{
    public async Task<OnboardingStatusDto> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _ = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current account could not be found.");

        var profile = await profileRepository.GetProfileAsync(userId, cancellationToken);
        var preferences = await profileRepository.GetPreferencesAsync(userId, cancellationToken);

        var missingFields = new List<string>();

        if (profile is null || string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            missingFields.Add("displayName");
        }

        if (profile is null || string.IsNullOrWhiteSpace(profile.HomeAreaZipCode))
        {
            missingFields.Add("homeAreaZipCode");
        }

        if (profile?.SocialGoal is null)
        {
            missingFields.Add("socialGoal");
        }

        if (preferences is null || preferences.CuisineTags.Count == 0)
        {
            missingFields.Add("cuisineTags");
        }

        if (preferences?.SpiceTolerance is null)
        {
            missingFields.Add("spiceTolerance");
        }

        return new OnboardingStatusDto(missingFields.Count == 0, missingFields);
    }
}