// Business rules for registration, login, token refresh, logout, and account deletion.
using System.Text.RegularExpressions;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Auth;

/// <summary>
/// Owns account and session workflows while keeping controllers thin.
/// </summary>
public sealed class AuthService(
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IClock clock)
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromHours(8);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly Regex ZipCodePattern = new("^[0-9]{5}$", RegexOptions.Compiled);

    public async Task<SessionDto> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var zipCode = request.ZipCode.Trim();
        var normalizedUsername = Normalize(username);
        var normalizedEmail = Normalize(email);

        ValidateZipCode(zipCode);

        if (await authRepository.UsernameExistsAsync(normalizedUsername, cancellationToken: cancellationToken))
        {
            throw ApiException.Conflict("That username is already in use.");
        }

        if (await authRepository.EmailExistsAsync(normalizedEmail, cancellationToken: cancellationToken))
        {
            throw ApiException.Conflict("That email address is already in use.");
        }

        var now = clock.UtcNow;
        var userId = Guid.NewGuid();

        var account = new UserAccount(
            userId,
            username,
            normalizedUsername,
            email,
            normalizedEmail,
            passwordHasher.HashPassword(request.Password),
            AccountStatus.Active,
            new[] { UserRole.User },
            now,
            now,
            null);

        await authRepository.CreateAccountAsync(account, cancellationToken);

        // Registration also seeds the default profile-side records expected by onboarding and discovery.
        await profileRepository.SaveProfileAsync(new UserProfile(userId, username, null, zipCode, null, now, now), cancellationToken);
        await profileRepository.SavePreferencesAsync(new UserPreferences(userId, Array.Empty<string>(), null, Array.Empty<string>(), Array.Empty<string>(), now), cancellationToken);
        await profileRepository.SavePrivacySettingsAsync(new PrivacySettings(userId, true, now), cancellationToken);

        return await CreateSessionAsync(account, cancellationToken);
    }

    public async Task<SessionDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var account = await authRepository.FindByUsernameOrEmailAsync(request.UsernameOrEmail, cancellationToken);

        if (account is null || account.Status != AccountStatus.Active || !passwordHasher.VerifyPassword(account.PasswordHash, request.Password))
        {
            throw ApiException.Unauthorized("Invalid username/email or password.");
        }

        return await CreateSessionAsync(account, cancellationToken);
    }

    public async Task<SessionDto> RefreshAsync(RefreshSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await authRepository.GetSessionByRefreshTokenAsync(request.RefreshToken.Trim(), cancellationToken);

        if (session is null || session.RevokedAtUtc.HasValue || session.RefreshExpiresAtUtc <= clock.UtcNow)
        {
            throw ApiException.Unauthorized("The refresh token is invalid or expired.");
        }

        var account = await authRepository.GetByIdAsync(session.UserId, cancellationToken);

        if (account is null || account.Status != AccountStatus.Active)
        {
            throw ApiException.Unauthorized("The refresh token does not map to an active account.");
        }

        // Refresh rotates the old session instead of extending it in place.
        await authRepository.RevokeSessionAsync(session.Id, clock.UtcNow, cancellationToken);
        return await CreateSessionAsync(account, cancellationToken);
    }

    public async Task LogoutAsync(Guid userId, string accessToken, CancellationToken cancellationToken = default)
    {
        var session = await authRepository.GetSessionByAccessTokenAsync(accessToken, cancellationToken);

        if (session is null || session.UserId != userId)
        {
            throw ApiException.Unauthorized();
        }

        await authRepository.RevokeSessionAsync(session.Id, clock.UtcNow, cancellationToken);
    }

    public async Task DeleteAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The current account could not be found.");

        if (account.Status == AccountStatus.Deleted)
        {
            return;
        }

        var now = clock.UtcNow;
        var deletedAccount = account with
        {
            Status = AccountStatus.Deleted,
            DeletedAtUtc = now,
            UpdatedAtUtc = now,
        };

        await authRepository.UpdateAccountAsync(deletedAccount, cancellationToken);
        await authRepository.RevokeAllSessionsForUserAsync(userId, now, cancellationToken);
    }

    private async Task<SessionDto> CreateSessionAsync(UserAccount account, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var session = new UserSession(
            Guid.NewGuid(),
            account.Id,
            tokenGenerator.GenerateToken(),
            tokenGenerator.GenerateToken(),
            now.Add(AccessTokenLifetime),
            now.Add(RefreshTokenLifetime),
            now,
            null);

        await authRepository.SaveSessionAsync(session, cancellationToken);

        return new SessionDto(
            session.AccessToken,
            session.RefreshToken,
            session.ExpiresAtUtc,
            new CurrentUserSummaryDto(account.Id, account.Username, account.Email, account.Roles));
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