using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Auth;

/// <summary>
/// SQLite-backed auth repository that persists accounts, roles, and opaque sessions.
/// </summary>
public sealed class SqliteAuthRepository(TasteBudzDbContext dbContext) : IAuthRepository
{
    public async Task<UserAccount?> FindByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(usernameOrEmail);
        var entity = await dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Status == AccountStatus.Active &&
                (user.NormalizedUsername == normalized || user.NormalizedEmail == normalized),
                cancellationToken);

        return entity is null ? null : await MapAccountAsync(entity, cancellationToken);
    }

    public async Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(username);
        var entity = await dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Status == AccountStatus.Active &&
                user.NormalizedUsername == normalized,
                cancellationToken);

        return entity is null ? null : await MapAccountAsync(entity, cancellationToken);
    }

    public async Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserAccounts.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        return entity is null ? null : await MapAccountAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserAccount>> ListActiveAccountsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.UserAccounts
            .AsNoTracking()
            .Where(account => account.Status == AccountStatus.Active)
            .OrderBy(account => account.Username)
            .ToListAsync(cancellationToken);
        var accountIds = entities.Select(account => account.Id).ToArray();
        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(role => accountIds.Contains(role.UserId))
            .ToListAsync(cancellationToken);

        return entities
            .Select(entity => MapAccount(entity, roles.Where(role => role.UserId == entity.Id)))
            .ToArray();
    }

    public Task<bool> UsernameExistsAsync(string normalizedUsername, Guid? excludingUserId = null, CancellationToken cancellationToken = default) =>
        dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user =>
                user.Status == AccountStatus.Active &&
                user.Id != excludingUserId &&
                user.NormalizedUsername == normalizedUsername,
                cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludingUserId = null, CancellationToken cancellationToken = default) =>
        dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user =>
                user.Status == AccountStatus.Active &&
                user.Id != excludingUserId &&
                user.NormalizedEmail == normalizedEmail,
                cancellationToken);

    public async Task<UserAccount> CreateAccountAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        dbContext.UserAccounts.Add(ToEntity(account));
        dbContext.UserRoles.AddRange(account.Roles.Select(role => new UserRoleEntity
        {
            UserId = account.Id,
            Role = role,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task UpdateAccountAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserAccounts.FirstOrDefaultAsync(user => user.Id == account.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.UserAccounts.Add(ToEntity(account));
        }
        else
        {
            entity.Username = account.Username;
            entity.NormalizedUsername = account.NormalizedUsername;
            entity.Email = account.Email;
            entity.NormalizedEmail = account.NormalizedEmail;
            entity.PasswordHash = account.PasswordHash;
            entity.Status = account.Status;
            entity.CreatedAtUtc = account.CreatedAtUtc;
            entity.UpdatedAtUtc = account.UpdatedAtUtc;
            entity.DeletedAtUtc = account.DeletedAtUtc;
        }

        var existingRoles = await dbContext.UserRoles.Where(role => role.UserId == account.Id).ToListAsync(cancellationToken);
        dbContext.UserRoles.RemoveRange(existingRoles);
        dbContext.UserRoles.AddRange(account.Roles.Select(role => new UserRoleEntity
        {
            UserId = account.Id,
            Role = role,
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> ListPermanentDeleteBlockersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var blockers = new List<string>();
        var targetMediaIds = dbContext.MediaAssets
            .Where(media => media.OwnerUserId == userId || media.ProfileUserId == userId)
            .Select(media => media.Id);

        if (await dbContext.Events.AnyAsync(item => item.HostUserId == userId, cancellationToken))
        {
            blockers.Add("hosted events");
        }

        if (await dbContext.EventParticipants.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            blockers.Add("event participation");
        }

        if (await dbContext.EventFeedbacks.AnyAsync(item => item.AuthorUserId == userId, cancellationToken))
        {
            blockers.Add("event feedback");
        }

        if (await dbContext.CheckoutSessions.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            blockers.Add("checkout sessions");
        }

        if (await dbContext.Groups.AnyAsync(item => item.OwnerUserId == userId, cancellationToken))
        {
            blockers.Add("owned groups");
        }

        if (await dbContext.GroupAnnouncements.AnyAsync(item => item.AuthorUserId == userId, cancellationToken))
        {
            blockers.Add("group announcements");
        }

        if (await dbContext.ChatMessages.AnyAsync(item => item.SenderUserId == userId, cancellationToken))
        {
            blockers.Add("chat messages");
        }

        if (await dbContext.ModerationReports.AnyAsync(item =>
                item.ReporterUserId == userId ||
                item.RelatedUserId == userId ||
                item.ResolvedByUserId == userId ||
                (item.TargetType == ReportTargetType.User && item.TargetId == userId),
                cancellationToken))
        {
            blockers.Add("moderation reports");
        }

        if (await dbContext.ModerationActions.AnyAsync(item => item.ActorUserId == userId, cancellationToken))
        {
            blockers.Add("moderation actions");
        }

        if (await dbContext.UserRestrictions.AnyAsync(item => item.IssuedByUserId == userId, cancellationToken))
        {
            blockers.Add("issued restrictions");
        }

        if (await dbContext.AuditLogEntries.AnyAsync(item => item.ActorUserId == userId, cancellationToken))
        {
            blockers.Add("audit log entries");
        }

        if (await dbContext.PasswordResetTokens.AnyAsync(item => item.CreatedByUserId == userId, cancellationToken))
        {
            blockers.Add("issued password reset tokens");
        }

        if (await dbContext.PasswordResetRequests.AnyAsync(item => item.ClosedByUserId == userId, cancellationToken))
        {
            blockers.Add("closed password reset requests");
        }

        if (await dbContext.MediaAssets.AnyAsync(item =>
                (item.OwnerUserId == userId || item.ProfileUserId == userId) &&
                (item.GroupId.HasValue || item.EventId.HasValue || item.ReportId.HasValue),
                cancellationToken))
        {
            blockers.Add("context-linked media assets");
        }

        if (await dbContext.EventFeedbackPhotos.AnyAsync(item => targetMediaIds.Contains(item.MediaAssetId), cancellationToken))
        {
            blockers.Add("event feedback photos");
        }

        return blockers;
    }

    public async Task PermanentlyDeleteAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.UserAccounts.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (account is null)
        {
            return;
        }

        var passwordResetRequests = await dbContext.PasswordResetRequests
            .Where(request => request.MatchedUserId == userId || request.ClosedByUserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var request in passwordResetRequests)
        {
            if (request.MatchedUserId == userId)
            {
                request.MatchedUserId = null;
            }

            if (request.ClosedByUserId == userId)
            {
                request.ClosedByUserId = null;
            }
        }

        await RemoveRangeAsync(dbContext.MediaAssets.Where(media =>
            (media.OwnerUserId == userId || media.ProfileUserId == userId) &&
            media.GroupId == null &&
            media.EventId == null &&
            media.ReportId == null), cancellationToken);
        await RemoveRangeAsync(dbContext.UserRoles.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.UserSessions.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.PasswordResetTokens.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.UserCuisinePreferences.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.UserDietaryFlags.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.UserAllergies.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.UserPreferences.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.PrivacySettings.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.RecurringAvailabilityWindows.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.OneOffAvailabilityWindows.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.SwipeDecisions.Where(item => item.ActorUserId == userId || item.SubjectUserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.BudConnections.Where(item => item.UserOneId == userId || item.UserTwoId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.UserBlocks.Where(item => item.BlockerUserId == userId || item.BlockedUserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.GroupMembers.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.GroupInvites.Where(item => item.InvitedUserId == userId || item.InviterUserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.RestaurantAdminAssignments.Where(item => item.UserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.Notifications.Where(item => item.RecipientUserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.UserRestrictions.Where(item => item.SubjectUserId == userId), cancellationToken);
        await RemoveRangeAsync(dbContext.UserProfiles.Where(item => item.UserId == userId), cancellationToken);

        dbContext.UserAccounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserSession?> GetSessionByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserSessions.AsNoTracking().FirstOrDefaultAsync(session => session.AccessToken == accessToken, cancellationToken);
        return entity is null ? null : MapSession(entity);
    }

    public async Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserSessions.AsNoTracking().FirstOrDefaultAsync(session => session.RefreshToken == refreshToken, cancellationToken);
        return entity is null ? null : MapSession(entity);
    }

    public async Task SaveSessionAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserSessions.FirstOrDefaultAsync(item => item.Id == session.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.UserSessions.Add(ToEntity(session));
        }
        else
        {
            entity.UserId = session.UserId;
            entity.AccessToken = session.AccessToken;
            entity.RefreshToken = session.RefreshToken;
            entity.ExpiresAtUtc = session.ExpiresAtUtc;
            entity.RefreshExpiresAtUtc = session.RefreshExpiresAtUtc;
            entity.CreatedAtUtc = session.CreatedAtUtc;
            entity.RevokedAtUtc = session.RevokedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeSessionAsync(Guid sessionId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserSessions.FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

        if (entity is not null)
        {
            entity.RevokedAtUtc = revokedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllSessionsForUserAsync(Guid userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default)
    {
        var sessions = await dbContext.UserSessions.Where(session => session.UserId == userId).ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.RevokedAtUtc = revokedAtUtc;
        }

        if (sessions.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        return entity is null ? null : MapPasswordResetToken(entity);
    }

    public async Task<IReadOnlyCollection<PasswordResetToken>> ListPasswordResetTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await dbContext.PasswordResetTokens
            .AsNoTracking()
            .Where(token => token.UserId == userId)
            .OrderByDescending(token => token.CreatedAtUtc)
            .ToListAsync(cancellationToken))
        .Select(MapPasswordResetToken)
        .ToArray();

    public async Task SavePasswordResetTokenAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PasswordResetTokens.FirstOrDefaultAsync(item => item.Id == token.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.PasswordResetTokens.Add(ToEntity(token));
        }
        else
        {
            entity.UserId = token.UserId;
            entity.TokenHash = token.TokenHash;
            entity.CreatedByUserId = token.CreatedByUserId;
            entity.CreatedAtUtc = token.CreatedAtUtc;
            entity.ExpiresAtUtc = token.ExpiresAtUtc;
            entity.UsedAtUtc = token.UsedAtUtc;
            entity.RevokedAtUtc = token.RevokedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeUnusedPasswordResetTokensForUserAsync(Guid userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedAtUtc == null && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = revokedAtUtc;
        }

        if (tokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PasswordResetRequest?> GetPasswordResetRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PasswordResetRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        return entity is null ? null : MapPasswordResetRequest(entity);
    }

    public async Task<IReadOnlyCollection<PasswordResetRequest>> ListOpenPasswordResetRequestsAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.PasswordResetRequests
            .AsNoTracking()
            .Where(request => request.ClosedAtUtc == null)
            .ToListAsync(cancellationToken))
        .OrderByDescending(request => request.CreatedAtUtc)
        .Select(MapPasswordResetRequest)
        .ToArray();

    public async Task SavePasswordResetRequestAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PasswordResetRequests.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.PasswordResetRequests.Add(ToEntity(request));
        }
        else
        {
            entity.Username = request.Username;
            entity.Message = request.Message;
            entity.MatchedUserId = request.MatchedUserId;
            entity.CreatedAtUtc = request.CreatedAtUtc;
            entity.ClosedAtUtc = request.ClosedAtUtc;
            entity.ClosedByUserId = request.ClosedByUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserAccount> MapAccountAsync(UserAccountEntity entity, CancellationToken cancellationToken)
    {
        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(role => role.UserId == entity.Id)
            .ToListAsync(cancellationToken);
        return MapAccount(entity, roles);
    }

    private static UserAccount MapAccount(UserAccountEntity entity, IEnumerable<UserRoleEntity> roles) =>
        new(
            entity.Id,
            entity.Username,
            entity.NormalizedUsername,
            entity.Email,
            entity.NormalizedEmail,
            entity.PasswordHash,
            entity.Status,
            roles.Select(role => role.Role).OrderBy(role => role).ToArray(),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.DeletedAtUtc);

    private static UserSession MapSession(UserSessionEntity entity) =>
        new(
            entity.Id,
            entity.UserId,
            entity.AccessToken,
            entity.RefreshToken,
            entity.ExpiresAtUtc,
            entity.RefreshExpiresAtUtc,
            entity.CreatedAtUtc,
            entity.RevokedAtUtc);

    private static PasswordResetToken MapPasswordResetToken(PasswordResetTokenEntity entity) =>
        new(
            entity.Id,
            entity.UserId,
            entity.TokenHash,
            entity.CreatedByUserId,
            entity.CreatedAtUtc,
            entity.ExpiresAtUtc,
            entity.UsedAtUtc,
            entity.RevokedAtUtc);

    private static PasswordResetRequest MapPasswordResetRequest(PasswordResetRequestEntity entity) =>
        new(
            entity.Id,
            entity.Username,
            entity.Message,
            entity.MatchedUserId,
            entity.CreatedAtUtc,
            entity.ClosedAtUtc,
            entity.ClosedByUserId);

    private static UserAccountEntity ToEntity(UserAccount account) =>
        new()
        {
            Id = account.Id,
            Username = account.Username,
            NormalizedUsername = account.NormalizedUsername,
            Email = account.Email,
            NormalizedEmail = account.NormalizedEmail,
            PasswordHash = account.PasswordHash,
            Status = account.Status,
            CreatedAtUtc = account.CreatedAtUtc,
            UpdatedAtUtc = account.UpdatedAtUtc,
            DeletedAtUtc = account.DeletedAtUtc,
        };

    private static UserSessionEntity ToEntity(UserSession session) =>
        new()
        {
            Id = session.Id,
            UserId = session.UserId,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            ExpiresAtUtc = session.ExpiresAtUtc,
            RefreshExpiresAtUtc = session.RefreshExpiresAtUtc,
            CreatedAtUtc = session.CreatedAtUtc,
            RevokedAtUtc = session.RevokedAtUtc,
        };

    private static PasswordResetTokenEntity ToEntity(PasswordResetToken token) =>
        new()
        {
            Id = token.Id,
            UserId = token.UserId,
            TokenHash = token.TokenHash,
            CreatedByUserId = token.CreatedByUserId,
            CreatedAtUtc = token.CreatedAtUtc,
            ExpiresAtUtc = token.ExpiresAtUtc,
            UsedAtUtc = token.UsedAtUtc,
            RevokedAtUtc = token.RevokedAtUtc,
        };

    private static PasswordResetRequestEntity ToEntity(PasswordResetRequest request) =>
        new()
        {
            Id = request.Id,
            Username = request.Username,
            Message = request.Message,
            MatchedUserId = request.MatchedUserId,
            CreatedAtUtc = request.CreatedAtUtc,
            ClosedAtUtc = request.ClosedAtUtc,
            ClosedByUserId = request.ClosedByUserId,
        };

    private async Task RemoveRangeAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken)
        where TEntity : class
    {
        dbContext.RemoveRange(await query.ToListAsync(cancellationToken));
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
