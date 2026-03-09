// In-memory moderation repository used by the MVP runtime and automated tests.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Moderation;

/// <summary>
/// Stores moderation-side records in the shared in-memory state store.
/// </summary>
public sealed class InMemoryModerationRepository(InMemoryTasteBudzStore store) : IModerationRepository
{
    public Task<ModerationReport?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.ModerationReports.TryGetValue(reportId, out var report);
            return Task.FromResult(report);
        }
    }

    public Task<IReadOnlyCollection<ModerationReport>> ListReportsAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.ModerationReports.Values
                .OrderByDescending(report => report.CreatedAtUtc)
                .ThenBy(report => report.Id)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ModerationReport>>(items);
        }
    }

    public Task SaveReportAsync(ModerationReport report, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.ModerationReports[report.Id] = report;
            return Task.CompletedTask;
        }
    }

    public Task SaveActionAsync(ModerationAction action, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.ModerationActions[action.Id] = action;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<ModerationAction>> ListActionsAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.ModerationActions.Values
                .OrderByDescending(action => action.CreatedAtUtc)
                .ThenBy(action => action.Id)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ModerationAction>>(items);
        }
    }

    public Task<UserRestriction?> GetRestrictionAsync(Guid restrictionId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.UserRestrictions.TryGetValue(restrictionId, out var restriction);
            return Task.FromResult(restriction);
        }
    }

    public Task<IReadOnlyCollection<UserRestriction>> ListRestrictionsAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.UserRestrictions.Values
                .OrderByDescending(restriction => restriction.StartsAtUtc)
                .ThenBy(restriction => restriction.Id)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<UserRestriction>>(items);
        }
    }

    public Task SaveRestrictionAsync(UserRestriction restriction, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.UserRestrictions[restriction.Id] = restriction;
            return Task.CompletedTask;
        }
    }

    public Task SaveAuditLogEntryAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.AuditLogEntries[entry.Id] = entry;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<AuditLogEntry>> ListAuditLogEntriesAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.AuditLogEntries.Values
                .OrderByDescending(entry => entry.CreatedAtUtc)
                .ThenBy(entry => entry.Id)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<AuditLogEntry>>(items);
        }
    }
}
