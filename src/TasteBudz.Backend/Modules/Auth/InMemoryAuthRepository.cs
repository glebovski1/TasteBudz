// In-memory account and session repository used by unit tests.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Auth;

/// <summary>
/// Stores auth data in the shared in-memory test store behind the repository boundary.
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

    public Task<IReadOnlyCollection<string>> ListPermanentDeleteBlockersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var blockers = new List<string>();
            var targetMediaIds = store.MediaAssets.Values
                .Where(media => media.OwnerUserId == userId || media.ProfileUserId == userId)
                .Select(media => media.Id)
                .ToHashSet();

            if (store.Events.Values.Any(item => item.HostUserId == userId))
            {
                blockers.Add("hosted events");
            }

            if (store.EventParticipants.Values.Any(item => item.UserId == userId))
            {
                blockers.Add("event participation");
            }

            if (store.EventFeedbacks.Values.Any(item => item.AuthorUserId == userId))
            {
                blockers.Add("event feedback");
            }

            if (store.CheckoutSessions.Values.Any(item => item.UserId == userId))
            {
                blockers.Add("checkout sessions");
            }

            if (store.Groups.Values.Any(item => item.OwnerUserId == userId))
            {
                blockers.Add("owned groups");
            }

            if (store.GroupAnnouncements.Values.Any(item => item.AuthorUserId == userId))
            {
                blockers.Add("group announcements");
            }

            if (store.ChatMessages.Values.Any(item => item.SenderUserId == userId))
            {
                blockers.Add("chat messages");
            }

            if (store.ModerationReports.Values.Any(item =>
                    item.ReporterUserId == userId ||
                    item.RelatedUserId == userId ||
                    item.ResolvedByUserId == userId ||
                    (item.TargetType == ReportTargetType.User && item.TargetId == userId)))
            {
                blockers.Add("moderation reports");
            }

            if (store.ModerationActions.Values.Any(item => item.ActorUserId == userId))
            {
                blockers.Add("moderation actions");
            }

            if (store.UserRestrictions.Values.Any(item => item.IssuedByUserId == userId))
            {
                blockers.Add("issued restrictions");
            }

            if (store.AuditLogEntries.Values.Any(item => item.ActorUserId == userId))
            {
                blockers.Add("audit log entries");
            }

            if (store.PasswordResetTokens.Values.Any(item => item.CreatedByUserId == userId))
            {
                blockers.Add("issued password reset tokens");
            }

            if (store.PasswordResetRequests.Values.Any(item => item.ClosedByUserId == userId))
            {
                blockers.Add("closed password reset requests");
            }

            if (store.MediaAssets.Values.Any(item =>
                    (item.OwnerUserId == userId || item.ProfileUserId == userId) &&
                    (item.GroupId.HasValue || item.EventId.HasValue || item.ReportId.HasValue)))
            {
                blockers.Add("context-linked media assets");
            }

            if (store.EventFeedbackPhotos.Values.Any(item => targetMediaIds.Contains(item.MediaAssetId)))
            {
                blockers.Add("event feedback photos");
            }

            return Task.FromResult<IReadOnlyCollection<string>>(blockers);
        }
    }

    public Task PermanentlyDeleteAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            foreach (var request in store.PasswordResetRequests.Values
                         .Where(item => item.MatchedUserId == userId || item.ClosedByUserId == userId)
                         .ToArray())
            {
                store.PasswordResetRequests[request.Id] = request with
                {
                    MatchedUserId = request.MatchedUserId == userId ? null : request.MatchedUserId,
                    ClosedByUserId = request.ClosedByUserId == userId ? null : request.ClosedByUserId,
                };
            }

            RemoveAll(store.MediaAssets, item =>
                (item.OwnerUserId == userId || item.ProfileUserId == userId) &&
                !item.GroupId.HasValue &&
                !item.EventId.HasValue &&
                !item.ReportId.HasValue);
            RemoveAll(store.Sessions, item => item.UserId == userId);
            RemoveAll(store.PasswordResetTokens, item => item.UserId == userId);
            store.Profiles.Remove(userId);
            store.Preferences.Remove(userId);
            store.PrivacySettings.Remove(userId);
            RemoveAll(store.RecurringAvailability, item => item.UserId == userId);
            RemoveAll(store.OneOffAvailability, item => item.UserId == userId);
            RemoveAll(store.SwipeDecisions, item => item.ActorUserId == userId || item.SubjectUserId == userId);
            RemoveAll(store.BudConnections, item => item.UserOneId == userId || item.UserTwoId == userId);
            RemoveAll(store.Blocks, item => item.BlockerUserId == userId || item.BlockedUserId == userId);
            RemoveAll(store.GroupMembers, item => item.UserId == userId);
            RemoveAll(store.GroupInvites, item => item.InvitedUserId == userId || item.InviterUserId == userId);
            RemoveAll(store.RestaurantAdminAssignments, item => item.UserId == userId);
            RemoveAll(store.Notifications, item => item.RecipientUserId == userId);
            RemoveAll(store.UserRestrictions, item => item.SubjectUserId == userId);
            store.UserAccounts.Remove(userId);

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

    public Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var token = store.PasswordResetTokens.Values.FirstOrDefault(existing =>
                string.Equals(existing.TokenHash, tokenHash, StringComparison.Ordinal));

            return Task.FromResult(token);
        }
    }

    public Task<IReadOnlyCollection<PasswordResetToken>> ListPasswordResetTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var tokens = store.PasswordResetTokens.Values
                .Where(token => token.UserId == userId)
                .OrderByDescending(token => token.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<PasswordResetToken>>(tokens);
        }
    }

    public Task SavePasswordResetTokenAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.PasswordResetTokens[token.Id] = token;
            return Task.CompletedTask;
        }
    }

    public Task RevokeUnusedPasswordResetTokensForUserAsync(Guid userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            foreach (var token in store.PasswordResetTokens.Values
                         .Where(existing => existing.UserId == userId && existing.UsedAtUtc is null && existing.RevokedAtUtc is null)
                         .ToArray())
            {
                store.PasswordResetTokens[token.Id] = token with { RevokedAtUtc = revokedAtUtc };
            }

            return Task.CompletedTask;
        }
    }

    public Task<PasswordResetRequest?> GetPasswordResetRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.PasswordResetRequests.TryGetValue(requestId, out var request);
            return Task.FromResult(request);
        }
    }

    public Task<IReadOnlyCollection<PasswordResetRequest>> ListOpenPasswordResetRequestsAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var requests = store.PasswordResetRequests.Values
                .Where(request => request.ClosedAtUtc is null)
                .OrderByDescending(request => request.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<PasswordResetRequest>>(requests);
        }
    }

    public Task SavePasswordResetRequestAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.PasswordResetRequests[request.Id] = request;
            return Task.CompletedTask;
        }
    }

    private static void RemoveAll<TKey, TValue>(Dictionary<TKey, TValue> items, Func<TValue, bool> predicate)
        where TKey : notnull
    {
        foreach (var key in items.Where(item => predicate(item.Value)).Select(item => item.Key).ToArray())
        {
            items.Remove(key);
        }
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
