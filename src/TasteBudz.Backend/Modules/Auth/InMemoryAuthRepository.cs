// In-memory account and session repository used by the MVP runtime and tests.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Auth;

/// <summary>
/// Stores auth data in the shared in-memory store behind the repository boundary.
/// </summary>
public sealed class InMemoryAuthRepository(InMemoryTasteBudzStore store) : IAuthRepository
{
    public Task<UserAccount?> FindByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(usernameOrEmail);

        lock (store.SyncRoot)
        {
            var account = store.UserAccounts.Values.FirstOrDefault(user =>
                user.Status == AccountStatus.Active &&
                (string.Equals(user.NormalizedUsername, normalized, StringComparison.Ordinal) ||
                 string.Equals(user.NormalizedEmail, normalized, StringComparison.Ordinal)));

            return Task.FromResult(account);
        }
    }

    public Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(username);

        lock (store.SyncRoot)
        {
            var account = store.UserAccounts.Values.FirstOrDefault(user =>
                user.Status == AccountStatus.Active &&
                string.Equals(user.NormalizedUsername, normalized, StringComparison.Ordinal));

            return Task.FromResult(account);
        }
    }

    public Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.UserAccounts.TryGetValue(userId, out var account);
            return Task.FromResult(account);
        }
    }

    public Task<IReadOnlyCollection<UserAccount>> ListActiveAccountsAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.UserAccounts.Values
                .Where(account => account.Status == AccountStatus.Active)
                .OrderBy(account => account.Username, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<UserAccount>>(items);
        }
    }

    public Task<bool> UsernameExistsAsync(string normalizedUsername, Guid? excludingUserId = null, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var exists = store.UserAccounts.Values.Any(user =>
                user.Status == AccountStatus.Active &&
                user.Id != excludingUserId &&
                string.Equals(user.NormalizedUsername, normalizedUsername, StringComparison.Ordinal));

            return Task.FromResult(exists);
        }
    }

    public Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludingUserId = null, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var exists = store.UserAccounts.Values.Any(user =>
                user.Status == AccountStatus.Active &&
                user.Id != excludingUserId &&
                string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal));

            return Task.FromResult(exists);
        }
    }

    public Task<UserAccount> CreateAccountAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.UserAccounts[account.Id] = account;
            return Task.FromResult(account);
        }
    }

    public Task UpdateAccountAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.UserAccounts[account.Id] = account;
            return Task.CompletedTask;
        }
    }

    public Task<UserSession?> GetSessionByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var session = store.Sessions.Values.FirstOrDefault(existing =>
                string.Equals(existing.AccessToken, accessToken, StringComparison.Ordinal));

            return Task.FromResult(session);
        }
    }

    public Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var session = store.Sessions.Values.FirstOrDefault(existing =>
                string.Equals(existing.RefreshToken, refreshToken, StringComparison.Ordinal));

            return Task.FromResult(session);
        }
    }

    public Task SaveSessionAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }

    public Task RevokeSessionAsync(Guid sessionId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            if (store.Sessions.TryGetValue(sessionId, out var session))
            {
                store.Sessions[sessionId] = session with { RevokedAtUtc = revokedAtUtc };
            }

            return Task.CompletedTask;
        }
    }

    public Task RevokeAllSessionsForUserAsync(Guid userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            foreach (var session in store.Sessions.Values.Where(existing => existing.UserId == userId).ToArray())
            {
                store.Sessions[session.Id] = session with { RevokedAtUtc = revokedAtUtc };
            }

            return Task.CompletedTask;
        }
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}