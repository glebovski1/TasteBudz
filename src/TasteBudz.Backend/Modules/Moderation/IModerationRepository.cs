// Persistence boundary for moderation reports, restrictions, and audit history.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Moderation;

/// <summary>
/// Stores moderation-side records behind a single module repository boundary.
/// </summary>
public interface IModerationRepository
{
    Task<ModerationReport?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ModerationReport>> ListReportsAsync(CancellationToken cancellationToken = default);

    Task SaveReportAsync(ModerationReport report, CancellationToken cancellationToken = default);

    Task SaveActionAsync(ModerationAction action, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ModerationAction>> ListActionsAsync(CancellationToken cancellationToken = default);

    Task<UserRestriction?> GetRestrictionAsync(Guid restrictionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserRestriction>> ListRestrictionsAsync(CancellationToken cancellationToken = default);

    Task SaveRestrictionAsync(UserRestriction restriction, CancellationToken cancellationToken = default);

    Task SaveAuditLogEntryAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AuditLogEntry>> ListAuditLogEntriesAsync(CancellationToken cancellationToken = default);
}
