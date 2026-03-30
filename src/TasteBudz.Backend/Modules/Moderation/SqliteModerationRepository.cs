using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Moderation;

/// <summary>
/// SQLite-backed repository for reports, restrictions, actions, and audit entries.
/// </summary>
public sealed class SqliteModerationRepository(TasteBudzDbContext dbContext) : IModerationRepository
{
    public async Task<ModerationReport?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ModerationReports.AsNoTracking().FirstOrDefaultAsync(report => report.Id == reportId, cancellationToken);
        return entity is null ? null : MapReport(entity);
    }

    public async Task<IReadOnlyCollection<ModerationReport>> ListReportsAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.ModerationReports.AsNoTracking().ToListAsync(cancellationToken))
        .Select(MapReport)
        .OrderByDescending(report => report.CreatedAtUtc)
        .ThenBy(report => report.Id)
        .ToArray();

    public async Task SaveReportAsync(ModerationReport report, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ModerationReports.FirstOrDefaultAsync(item => item.Id == report.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.ModerationReports.Add(ToEntity(report));
        }
        else
        {
            entity.ReporterUserId = report.ReporterUserId;
            entity.TargetType = report.TargetType;
            entity.TargetId = report.TargetId;
            entity.Category = report.Category;
            entity.Reason = report.Reason;
            entity.Explanation = report.Explanation;
            entity.RelatedEventId = report.RelatedEventId;
            entity.RelatedUserId = report.RelatedUserId;
            entity.RelatedMessageId = report.RelatedMessageId;
            entity.CreatedAtUtc = report.CreatedAtUtc;
            entity.Status = report.Status;
            entity.ResolvedByUserId = report.ResolvedByUserId;
            entity.ResolvedAtUtc = report.ResolvedAtUtc;
            entity.ResolutionDecision = report.ResolutionDecision;
            entity.ResolutionNotes = report.ResolutionNotes;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveActionAsync(ModerationAction action, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ModerationActions.FirstOrDefaultAsync(item => item.Id == action.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.ModerationActions.Add(ToEntity(action));
        }
        else
        {
            entity.ActorUserId = action.ActorUserId;
            entity.ReportId = action.ReportId;
            entity.ActionType = action.ActionType;
            entity.Notes = action.Notes;
            entity.CreatedAtUtc = action.CreatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ModerationAction>> ListActionsAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.ModerationActions.AsNoTracking().ToListAsync(cancellationToken))
        .Select(MapAction)
        .OrderByDescending(action => action.CreatedAtUtc)
        .ThenBy(action => action.Id)
        .ToArray();

    public async Task<UserRestriction?> GetRestrictionAsync(Guid restrictionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserRestrictions.AsNoTracking().FirstOrDefaultAsync(restriction => restriction.Id == restrictionId, cancellationToken);
        return entity is null ? null : MapRestriction(entity);
    }

    public async Task<IReadOnlyCollection<UserRestriction>> ListRestrictionsAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.UserRestrictions.AsNoTracking().ToListAsync(cancellationToken))
        .Select(MapRestriction)
        .OrderByDescending(restriction => restriction.StartsAtUtc)
        .ThenBy(restriction => restriction.Id)
        .ToArray();

    public async Task SaveRestrictionAsync(UserRestriction restriction, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserRestrictions.FirstOrDefaultAsync(item => item.Id == restriction.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.UserRestrictions.Add(ToEntity(restriction));
        }
        else
        {
            entity.SubjectUserId = restriction.SubjectUserId;
            entity.IssuedByUserId = restriction.IssuedByUserId;
            entity.Scope = restriction.Scope;
            entity.Reason = restriction.Reason;
            entity.StartsAtUtc = restriction.StartsAtUtc;
            entity.ExpiresAtUtc = restriction.ExpiresAtUtc;
            entity.Status = restriction.Status;
            entity.RevokedAtUtc = restriction.RevokedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAuditLogEntryAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AuditLogEntries.FirstOrDefaultAsync(item => item.Id == entry.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.AuditLogEntries.Add(ToEntity(entry));
        }
        else
        {
            entity.ActionType = entry.ActionType;
            entity.ActorUserId = entry.ActorUserId;
            entity.TargetEntityType = entry.TargetEntityType;
            entity.TargetEntityId = entry.TargetEntityId;
            entity.CreatedAtUtc = entry.CreatedAtUtc;
            entity.Details = entry.Details;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditLogEntry>> ListAuditLogEntriesAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.AuditLogEntries.AsNoTracking().ToListAsync(cancellationToken))
        .Select(MapAuditLogEntry)
        .OrderByDescending(entry => entry.CreatedAtUtc)
        .ThenBy(entry => entry.Id)
        .ToArray();

    private static ModerationReport MapReport(ModerationReportEntity entity) =>
        new(
            entity.Id,
            entity.ReporterUserId,
            entity.TargetType,
            entity.TargetId,
            entity.Category,
            entity.Reason,
            entity.Explanation,
            entity.RelatedEventId,
            entity.RelatedUserId,
            entity.RelatedMessageId,
            entity.CreatedAtUtc,
            entity.Status,
            entity.ResolvedByUserId,
            entity.ResolvedAtUtc,
            entity.ResolutionDecision,
            entity.ResolutionNotes);

    private static ModerationAction MapAction(ModerationActionEntity entity) =>
        new(
            entity.Id,
            entity.ActorUserId,
            entity.ReportId,
            entity.ActionType,
            entity.Notes,
            entity.CreatedAtUtc);

    private static UserRestriction MapRestriction(UserRestrictionEntity entity) =>
        new(
            entity.Id,
            entity.SubjectUserId,
            entity.IssuedByUserId,
            entity.Scope,
            entity.Reason,
            entity.StartsAtUtc,
            entity.ExpiresAtUtc,
            entity.Status,
            entity.RevokedAtUtc);

    private static AuditLogEntry MapAuditLogEntry(AuditLogEntryEntity entity) =>
        new(
            entity.Id,
            entity.ActionType,
            entity.ActorUserId,
            entity.TargetEntityType,
            entity.TargetEntityId,
            entity.CreatedAtUtc,
            entity.Details);

    private static ModerationReportEntity ToEntity(ModerationReport item) =>
        new()
        {
            Id = item.Id,
            ReporterUserId = item.ReporterUserId,
            TargetType = item.TargetType,
            TargetId = item.TargetId,
            Category = item.Category,
            Reason = item.Reason,
            Explanation = item.Explanation,
            RelatedEventId = item.RelatedEventId,
            RelatedUserId = item.RelatedUserId,
            RelatedMessageId = item.RelatedMessageId,
            CreatedAtUtc = item.CreatedAtUtc,
            Status = item.Status,
            ResolvedByUserId = item.ResolvedByUserId,
            ResolvedAtUtc = item.ResolvedAtUtc,
            ResolutionDecision = item.ResolutionDecision,
            ResolutionNotes = item.ResolutionNotes,
        };

    private static ModerationActionEntity ToEntity(ModerationAction item) =>
        new()
        {
            Id = item.Id,
            ActorUserId = item.ActorUserId,
            ReportId = item.ReportId,
            ActionType = item.ActionType,
            Notes = item.Notes,
            CreatedAtUtc = item.CreatedAtUtc,
        };

    private static UserRestrictionEntity ToEntity(UserRestriction item) =>
        new()
        {
            Id = item.Id,
            SubjectUserId = item.SubjectUserId,
            IssuedByUserId = item.IssuedByUserId,
            Scope = item.Scope,
            Reason = item.Reason,
            StartsAtUtc = item.StartsAtUtc,
            ExpiresAtUtc = item.ExpiresAtUtc,
            Status = item.Status,
            RevokedAtUtc = item.RevokedAtUtc,
        };

    private static AuditLogEntryEntity ToEntity(AuditLogEntry item) =>
        new()
        {
            Id = item.Id,
            ActionType = item.ActionType,
            ActorUserId = item.ActorUserId,
            TargetEntityType = item.TargetEntityType,
            TargetEntityId = item.TargetEntityId,
            CreatedAtUtc = item.CreatedAtUtc,
            Details = item.Details,
        };
}
