// Business rules for reading and updating the current user's profile.
using System.Text.RegularExpressions;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Media;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Owns profile retrieval and update validation.
/// </summary>
public sealed class ProfileService(
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    IMediaRepository mediaRepository,
    IClock clock)
{
    private static readonly Regex ZipCodePattern = new("^[0-9]{5}$", RegexOptions.Compiled);

    public async Task<ProfileDto> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current account could not be found.");
        var profile = await profileRepository.GetProfileAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current profile could not be found.");

        return await ToDtoAsync(account, profile, includeEmail: true, cancellationToken);
    }

    /// <summary>
    /// Returns a public profile for any user by ID.
    /// Email is intentionally blanked — callers only need display info.
    /// </summary>
    public async Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");
        var profile = await profileRepository.GetProfileAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");

        return await ToDtoAsync(account, profile, includeEmail: false, cancellationToken);
    }

    public async Task<ProfileDto> UpdateMyProfileAsync(Guid userId, UpdateMyProfileRequest request, CancellationToken cancellationToken = default)
    {
        var account = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current account could not be found.");
        var profile = await profileRepository.GetProfileAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current profile could not be found.");

        var now = clock.UtcNow;
        var updatedAccount = account;

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var username = request.Username.Trim();
            var normalizedUsername = Normalize(username);

            if (await authRepository.UsernameExistsAsync(normalizedUsername, userId, cancellationToken))
            {
                throw ApiException.Conflict("That username is already in use.");
            }

            updatedAccount = updatedAccount with
            {
                Username = username,
                NormalizedUsername = normalizedUsername,
                UpdatedAtUtc = now,
            };
        }

        if (!string.IsNullOrWhiteSpace(request.HomeAreaZipCode))
        {
            ValidateZipCode(request.HomeAreaZipCode.Trim());
        }

        var updatedProfile = profile with
        {
            DisplayName = request.DisplayName?.Trim() ?? profile.DisplayName,
            Bio = request.Bio is null ? profile.Bio : string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
            HomeAreaZipCode = request.HomeAreaZipCode?.Trim() ?? profile.HomeAreaZipCode,
            SocialGoal = request.SocialGoal ?? profile.SocialGoal,
            UpdatedAtUtc = now,
        };

        await authRepository.UpdateAccountAsync(updatedAccount, cancellationToken);
        await profileRepository.SaveProfileAsync(updatedProfile, cancellationToken);

        return await ToDtoAsync(updatedAccount, updatedProfile, includeEmail: true, cancellationToken);
    }

    private async Task<ProfileDto> ToDtoAsync(
        UserAccount account,
        UserProfile profile,
        bool includeEmail,
        CancellationToken cancellationToken)
    {
        var avatarTask = mediaRepository.GetProfileAvatarAsync(account.Id, cancellationToken);
        var preferencesTask = profileRepository.GetPreferencesAsync(account.Id, cancellationToken);
        await Task.WhenAll(avatarTask, preferencesTask);

        var avatar = await avatarTask;
        var preferences = await preferencesTask;

        return new ProfileDto(
            account.Id,
            account.Username,
            includeEmail ? account.Email : string.Empty,
            profile.DisplayName,
            profile.Bio,
            profile.HomeAreaZipCode,
            profile.SocialGoal,
            avatar?.Id,
            preferences?.CuisineTags ?? Array.Empty<string>(),
            preferences?.DietaryFlags ?? Array.Empty<string>());
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static void ValidateZipCode(string zipCode)
    {
        if (!ZipCodePattern.IsMatch(zipCode))
        {
            throw ApiException.BadRequest("ZIP code must be a 5-digit value.");
        }
    }
}
