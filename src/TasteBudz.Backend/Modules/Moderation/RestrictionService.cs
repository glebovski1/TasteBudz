// Scoped restriction workflows and enforcement helpers.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Backend.Modules.Moderation;

/// <summary>
/// Applies, updates, and evaluates active user restrictions.
/// </summary>
public sealed class RestrictionService(
    IModerationRepository moderationRepository,
    IAuthRepository authRepository,
    AuditLogService auditLogService,
    IClock clock)
{
    public async Task<RestrictionDto> CreateAsync(CurrentUser actor, CreateRestrictionRequest request, CancellationToken cancellationToken = default)
    {
        var subjectUserId = request.SubjectUserId ?? throw ApiException.BadRequest("subjectUserId is required.");
        var scope = request.Scope ?? throw ApiException.BadRequest("scope is required.");
        var reason = NormalizeReason(request.Reason);

        _ = await authRepository.GetByIdAsync(subjectUserId, cancellationToken)
            ?? throw ApiException.NotFound("The restricted user could not be found.");

        var now = clock.UtcNow;
        var restriction = new UserRestriction(
            Guid.NewGuid(),
            subjectUserId,
            actor.UserId,
            scope,
            reason,
            now,
            request.ExpiresAtUtc,
            RestrictionStatus.Active,
            null);

        await moderationRepository.SaveRestrictionAsync(restriction, cancellationToken);
        await moderationRepository.SaveActionAsync(
            new ModerationAction(Guid.NewGuid(), actor.UserId, null, ModerationActionType.RestrictionCreated, reason, now),
            cancellationToken);
        await auditLogService.WriteAsync(
            new AuditLogEntry(Guid.NewGuid(), "RestrictionCreated", actor.UserId, nameof(UserRestriction), restriction.Id, now, $"{scope}:{reason}"),
            cancellationToken);

        return ToDto(restriction);
    }

    public async Task<RestrictionDto> UpdateAsync(CurrentUser actor, Guid restrictionId, UpdateRestrictionRequest request, CancellationToken cancellationToken = default)
    {
        var restriction = await moderationRepository.GetRestrictionAsync(restrictionId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restriction could not be found.");
        var evaluated = await EvaluateStatusAsync(restriction, cancellationToken);
        var now = clock.UtcNow;
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? evaluated.Reason : request.Reason.Trim();
        UserRestriction updated;
        ModerationActionType actionType;
        string auditActionType;

        if (request.Revoke == true)
        {
            updated = evaluated with
            {
                Status = RestrictionStatus.Revoked,
                RevokedAtUtc = now,
                ExpiresAtUtc = request.ExpiresAtUtc ?? evaluated.ExpiresAtUtc,
                Reason = reason,
            };
            actionType = ModerationActionType.RestrictionRevoked;
            auditActionType = "RestrictionRevoked";
        }
        else
        {
            updated = evaluated with
            {
                ExpiresAtUtc = request.ExpiresAtUtc ?? evaluated.ExpiresAtUtc,
                Reason = reason,
                Status = evaluated.Status == RestrictionStatus.Revoked ? RestrictionStatus.Revoked : RestrictionStatus.Active,
            };
            actionType = ModerationActionType.RestrictionUpdated;
            auditActionType = "RestrictionUpdated";
        }

        await moderationRepository.SaveRestrictionAsync(updated, cancellationToken);
        await moderationRepository.SaveActionAsync(
            new ModerationAction(Guid.NewGuid(), actor.UserId, null, actionType, reason, now),
            cancellationToken);
        await auditLogService.WriteAsync(
            new AuditLogEntry(Guid.NewGuid(), auditActionType, actor.UserId, nameof(UserRestriction), updated.Id, now, $"{updated.Scope}:{reason}"),
            cancellationToken);

        return ToDto(updated);
    }

    public async Task<bool> IsRestrictedAsync(Guid subjectUserId, RestrictionScope scope, CancellationToken cancellationToken = default)
    {
        var restrictions = await moderationRepository.ListRestrictionsAsync(cancellationToken);

        foreach (var restriction in restrictions.Where(candidate =>
            candidate.SubjectUserId == subjectUserId &&
            candidate.Scope == scope))
        {
            var evaluated = await EvaluateStatusAsync(restriction, cancellationToken);

            if (evaluated.Status == RestrictionStatus.Active &&
                evaluated.StartsAtUtc <= clock.UtcNow &&
                (!evaluated.ExpiresAtUtc.HasValue || evaluated.ExpiresAtUtc > clock.UtcNow))
            {
                return true;
            }
        }

        return false;
    }

    public async Task EnsureNotRestrictedAsync(Guid subjectUserId, RestrictionScope scope, string detail, CancellationToken cancellationToken = default)
    {
        if (await IsRestrictedAsync(subjectUserId, scope, cancellationToken))
        {
            throw ApiException.Forbidden(detail);
        }
    }

    private async Task<UserRestriction> EvaluateStatusAsync(UserRestriction restriction, CancellationToken cancellationToken)
    {
        if (restriction.Status != RestrictionStatus.Active)
        {
            return restriction;
        }

        if (restriction.ExpiresAtUtc.HasValue && restriction.ExpiresAtUtc <= clock.UtcNow)
        {
            var expired = restriction with { Status = RestrictionStatus.Expired };
            await moderationRepository.SaveRestrictionAsync(expired, cancellationToken);
            return expired;
        }

        return restriction;
    }

    private static RestrictionDto ToDto(UserRestriction restriction) =>
        new(
            restriction.Id,
            restriction.SubjectUserId,
            restriction.Scope,
            restriction.Reason,
            restriction.StartsAtUtc,
            restriction.ExpiresAtUtc,
            restriction.Status,
            restriction.RevokedAtUtc);

    private static string NormalizeReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw ApiException.BadRequest("reason is required.");
        }

        return value.Trim();
    }
}
