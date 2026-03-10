// Business rules for reading and updating the current user's profile.
using System.Text.RegularExpressions;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Owns profile retrieval and update validation.
/// </summary>
public sealed class ProfileService(
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    IClock clock)
{
    private static readonly Regex ZipCodePattern = new("^[0-9]{5}$", RegexOptions.Compiled);

    public async Task<ProfileDto> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current account could not be found.");
        var profile = await profileRepository.GetProfileAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current profile could not be found.");

        return ToDto(account, profile);
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

        return ToDto(updatedAccount, updatedProfile);
    }

    private static ProfileDto ToDto(UserAccount account, UserProfile profile) =>
        new(account.Id, account.Username, account.Email, profile.DisplayName, profile.Bio, profile.HomeAreaZipCode, profile.SocialGoal);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static void ValidateZipCode(string zipCode)
    {
        if (!ZipCodePattern.IsMatch(zipCode))
        {
            throw ApiException.BadRequest("ZIP code must be a 5-digit value.");
        }
    }
}