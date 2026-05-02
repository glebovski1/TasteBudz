// Business rules for registration, login, token refresh, logout, and account deletion.
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Moderation;
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
    IClock clock,
    IHttpContextAccessor? httpContextAccessor = null,
    AuditLogService? auditLogService = null,
    IPersistenceTransactionRunner? transactionRunner = null,
    RestrictionService? restrictionService = null)
{
    private readonly IPersistenceTransactionRunner persistenceTransactionRunner = transactionRunner ?? NoOpPersistenceTransactionRunner.Instance;
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromHours(8);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(24);
    private static readonly Regex ZipCodePattern = new("^[0-9]{5}$", RegexOptions.Compiled);
    private const string PasswordResetRequestAcceptanceMessage = "If that username belongs to an active account, the admin team will review the request.";

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

        return await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await authRepository.CreateAccountAsync(account, cancellationToken);
                await profileRepository.SaveProfileAsync(new UserProfile(userId, username, null, zipCode, null, now, now), cancellationToken);
                await profileRepository.SavePreferencesAsync(new UserPreferences(userId, Array.Empty<string>(), null, Array.Empty<string>(), Array.Empty<string>(), now), cancellationToken);
                await profileRepository.SavePrivacySettingsAsync(new PrivacySettings(userId, true, now), cancellationToken);
                return await CreateSessionAsync(account, cancellationToken);
            },
            cancellationToken);
    }

    public async Task<SessionDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var account = await authRepository.FindByUsernameOrEmailAsync(request.UsernameOrEmail, cancellationToken);

        if (account is null || account.Status != AccountStatus.Active || !passwordHasher.VerifyPassword(account.PasswordHash, request.Password))
        {
            throw ApiException.Unauthorized("Invalid username/email or password.");
        }

        if (restrictionService is not null &&
            await restrictionService.IsFullBanActiveAsync(account.Id, cancellationToken))
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

        if (restrictionService is not null &&
            await restrictionService.IsFullBanActiveAsync(account.Id, cancellationToken))
        {
            throw ApiException.Unauthorized("The refresh token does not map to an active account.");
        }

        return await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await authRepository.RevokeSessionAsync(session.Id, clock.UtcNow, cancellationToken);
                return await CreateSessionAsync(account, cancellationToken);
            },
            cancellationToken);
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

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await authRepository.UpdateAccountAsync(deletedAccount, cancellationToken);
                await authRepository.RevokeAllSessionsForUserAsync(userId, now, cancellationToken);
            },
            cancellationToken);
    }

    public async Task DeleteAccountAsAdminAsync(CurrentUser admin, Guid userId, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(admin);

        if (userId == admin.UserId)
        {
            throw ApiException.BadRequest("Admins cannot delete their own account from the admin panel.");
        }

        var account = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");

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

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await authRepository.UpdateAccountAsync(deletedAccount, cancellationToken);
                await authRepository.RevokeAllSessionsForUserAsync(userId, now, cancellationToken);

                if (auditLogService is not null)
                {
                    await auditLogService.WriteAsync(
                        new AuditLogEntry(Guid.NewGuid(), "AdminUserSoftDeleted", admin.UserId, nameof(UserAccount), userId, now, "Admin soft-deleted a user account."),
                        cancellationToken);
                }
            },
            cancellationToken);
    }

    public async Task PermanentlyDeleteAccountAsAdminAsync(
        CurrentUser admin,
        Guid userId,
        PermanentlyDeleteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(admin);

        if (userId == admin.UserId)
        {
            throw ApiException.BadRequest("Admins cannot permanently delete their own account from the admin panel.");
        }

        if (!string.Equals(request.Confirmation, "delete", StringComparison.Ordinal))
        {
            throw ApiException.BadRequest("Type delete to permanently delete the user.");
        }

        var account = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");

        if (account.Status != AccountStatus.Deleted)
        {
            throw ApiException.Conflict("Only soft-deleted users can be permanently deleted.");
        }

        var blockers = await authRepository.ListPermanentDeleteBlockersAsync(userId, cancellationToken);

        if (blockers.Count > 0)
        {
            throw ApiException.Conflict($"This user still has historical records and cannot be permanently deleted. Blocking records: {string.Join(", ", blockers)}.");
        }

        var now = clock.UtcNow;

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await authRepository.PermanentlyDeleteAccountAsync(userId, cancellationToken);

                if (auditLogService is not null)
                {
                    await auditLogService.WriteAsync(
                        new AuditLogEntry(Guid.NewGuid(), "AdminUserPermanentlyDeleted", admin.UserId, nameof(UserAccount), userId, now, "Admin permanently deleted a soft-deleted user account."),
                        cancellationToken);
                }
            },
            cancellationToken);
    }

    public async Task<PasswordResetTokenDto> CreatePasswordResetTokenAsync(
        CurrentUser admin,
        CreatePasswordResetTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!admin.Roles.Contains(UserRole.Admin))
        {
            throw ApiException.Forbidden("Only admins can create password reset tokens.");
        }

        var passwordResetRequest = request.PasswordResetRequestId.HasValue
            ? await authRepository.GetPasswordResetRequestAsync(request.PasswordResetRequestId.Value, cancellationToken)
                ?? throw ApiException.NotFound("The requested password reset request could not be found.")
            : null;

        if (passwordResetRequest?.ClosedAtUtc is not null)
        {
            throw ApiException.Conflict("That password reset request is already closed.");
        }

        var account = await ResolvePasswordResetTargetAccountAsync(request, passwordResetRequest, cancellationToken);

        var now = clock.UtcNow;
        var rawToken = tokenGenerator.GenerateToken();
        var resetToken = new PasswordResetToken(
            Guid.NewGuid(),
            account.Id,
            HashToken(rawToken),
            admin.UserId,
            now,
            now.Add(PasswordResetTokenLifetime),
            null,
            null);

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await authRepository.RevokeUnusedPasswordResetTokensForUserAsync(account.Id, now, cancellationToken);
                await authRepository.SavePasswordResetTokenAsync(resetToken, cancellationToken);
                if (passwordResetRequest is not null)
                {
                    await authRepository.SavePasswordResetRequestAsync(
                        passwordResetRequest with
                        {
                            ClosedAtUtc = now,
                            ClosedByUserId = admin.UserId,
                        },
                        cancellationToken);
                }

                if (auditLogService is not null)
                {
                    await auditLogService.WriteAsync(
                        new AuditLogEntry(Guid.NewGuid(), "PasswordResetTokenIssued", admin.UserId, nameof(UserAccount), account.Id, now, "Admin issued a one-time password reset token."),
                        cancellationToken);
                }
            },
            cancellationToken);

        return new PasswordResetTokenDto(
            account.Id,
            account.Username,
            rawToken,
            BuildPasswordResetUrl(rawToken),
            resetToken.ExpiresAtUtc);
    }

    public async Task<PasswordResetRequestAcceptedDto> CreatePasswordResetRequestAsync(
        CreatePasswordResetRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var username = string.IsNullOrWhiteSpace(request.Username)
            ? throw ApiException.BadRequest("username is required.")
            : request.Username.Trim();
        var message = string.IsNullOrWhiteSpace(request.Message)
            ? throw ApiException.BadRequest("message is required.")
            : request.Message.Trim();
        var matchedAccount = await authRepository.FindByUsernameAsync(username, cancellationToken);
        var passwordResetRequest = new PasswordResetRequest(
            Guid.NewGuid(),
            username,
            message,
            matchedAccount?.Id,
            clock.UtcNow,
            null,
            null);

        await authRepository.SavePasswordResetRequestAsync(passwordResetRequest, cancellationToken);

        return new PasswordResetRequestAcceptedDto(PasswordResetRequestAcceptanceMessage);
    }

    public async Task<IReadOnlyCollection<PasswordResetRequestDto>> ListOpenPasswordResetRequestsAsync(
        CurrentUser admin,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(admin);

        var requests = await authRepository.ListOpenPasswordResetRequestsAsync(cancellationToken);
        var accounts = (await authRepository.ListActiveAccountsAsync(cancellationToken)).ToDictionary(account => account.Id);

        return requests
            .Select(request => MapPasswordResetRequest(request, accounts))
            .ToArray();
    }

    public async Task<PasswordResetRequestDto> ClosePasswordResetRequestAsync(
        CurrentUser admin,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(admin);

        var existing = await authRepository.GetPasswordResetRequestAsync(requestId, cancellationToken)
            ?? throw ApiException.NotFound("The requested password reset request could not be found.");
        var closed = existing.ClosedAtUtc.HasValue
            ? existing
            : existing with
            {
                ClosedAtUtc = clock.UtcNow,
                ClosedByUserId = admin.UserId,
            };

        if (!existing.ClosedAtUtc.HasValue)
        {
            await authRepository.SavePasswordResetRequestAsync(closed, cancellationToken);
        }

        var accounts = (await authRepository.ListActiveAccountsAsync(cancellationToken)).ToDictionary(account => account.Id);
        return MapPasswordResetRequest(closed, accounts);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var rawToken = string.IsNullOrWhiteSpace(request.Token)
            ? throw ApiException.BadRequest("token is required.")
            : request.Token.Trim();
        ValidatePassword(request.NewPassword);

        var tokenHash = HashToken(rawToken);
        var resetToken = await authRepository.GetPasswordResetTokenByHashAsync(tokenHash, cancellationToken)
            ?? throw ApiException.BadRequest("The password reset token is invalid or expired.");
        var now = clock.UtcNow;

        if (resetToken.UsedAtUtc.HasValue ||
            resetToken.RevokedAtUtc.HasValue ||
            resetToken.ExpiresAtUtc <= now)
        {
            throw ApiException.BadRequest("The password reset token is invalid or expired.");
        }

        var account = await authRepository.GetByIdAsync(resetToken.UserId, cancellationToken)
            ?? throw ApiException.BadRequest("The password reset token is invalid or expired.");

        if (account.Status != AccountStatus.Active)
        {
            throw ApiException.BadRequest("The password reset token is invalid or expired.");
        }

        var updated = account with
        {
            PasswordHash = passwordHasher.HashPassword(request.NewPassword),
            UpdatedAtUtc = now,
        };

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await authRepository.UpdateAccountAsync(updated, cancellationToken);
                await authRepository.SavePasswordResetTokenAsync(resetToken with { UsedAtUtc = now }, cancellationToken);
                await authRepository.RevokeAllSessionsForUserAsync(account.Id, now, cancellationToken);

                if (auditLogService is not null)
                {
                    await auditLogService.WriteAsync(
                        new AuditLogEntry(Guid.NewGuid(), "PasswordResetCompleted", account.Id, nameof(UserAccount), account.Id, now, "User completed password reset with an admin-issued token."),
                        cancellationToken);
                }
            },
            cancellationToken);
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

    private async Task<UserAccount> ResolvePasswordResetTargetAccountAsync(
        CreatePasswordResetTokenRequest request,
        PasswordResetRequest? passwordResetRequest,
        CancellationToken cancellationToken)
    {
        if (passwordResetRequest?.MatchedUserId is Guid matchedUserId)
        {
            return await authRepository.GetByIdAsync(matchedUserId, cancellationToken)
                ?? throw ApiException.NotFound("The requested user could not be found.");
        }

        var usernameOrEmail = !string.IsNullOrWhiteSpace(request.UsernameOrEmail)
            ? request.UsernameOrEmail.Trim()
            : passwordResetRequest is not null
                ? passwordResetRequest.Username
                : throw ApiException.BadRequest("usernameOrEmail is required.");

        return await authRepository.FindByUsernameOrEmailAsync(usernameOrEmail, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");
    }

    private static PasswordResetRequestDto MapPasswordResetRequest(
        PasswordResetRequest request,
        IReadOnlyDictionary<Guid, UserAccount> accounts) =>
        new(
            request.Id,
            request.Username,
            request.Message,
            request.MatchedUserId,
            request.MatchedUserId.HasValue && accounts.TryGetValue(request.MatchedUserId.Value, out var matchedAccount)
                ? matchedAccount.Username
                : null,
            request.CreatedAtUtc,
            request.ClosedAtUtc,
            request.ClosedByUserId);

    private static void EnsureAdmin(CurrentUser admin)
    {
        if (!admin.Roles.Contains(UserRole.Admin))
        {
            throw ApiException.Forbidden("Only admins can manage password reset requests.");
        }
    }

    private string BuildPasswordResetUrl(string rawToken)
    {
        var request = httpContextAccessor?.HttpContext?.Request
            ?? throw new InvalidOperationException("Password reset URLs require an active HTTP request context.");

        return UriHelper.BuildAbsolute(
            request.Scheme,
            request.Host,
            request.PathBase,
            "/Account/ResetPassword",
            QueryString.Create("token", rawToken));
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw ApiException.BadRequest("Password must be at least 8 characters long.");
        }
    }

    private static void ValidateZipCode(string zipCode)
    {
        if (!ZipCodePattern.IsMatch(zipCode))
        {
            throw ApiException.BadRequest("ZIP code must be a 5-digit value.");
        }
    }
}
