// Persistence boundary for accounts and sessions.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Auth;

/// <summary>
/// Stores and retrieves user accounts plus session tokens.
/// </summary>
public interface IAuthRepository
{
    Task<UserAccount?> FindByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);

    Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserAccount>> ListActiveAccountsAsync(CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string normalizedUsername, Guid? excludingUserId = null, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludingUserId = null, CancellationToken cancellationToken = default);

    Task<UserAccount> CreateAccountAsync(UserAccount account, CancellationToken cancellationToken = default);

    Task UpdateAccountAsync(UserAccount account, CancellationToken cancellationToken = default);

    Task<UserSession?> GetSessionByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task SaveSessionAsync(UserSession session, CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(Guid sessionId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default);

    Task RevokeAllSessionsForUserAsync(Guid userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default);
}