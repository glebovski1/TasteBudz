// Audit-log persistence and query helpers for sensitive actions.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Moderation;

/// <summary>
/// Writes append-only audit entries and exposes the admin audit query surface.
/// </summary>
public sealed class AuditLogService(IModerationRepository moderationRepository)
{
    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default) =>
        moderationRepository.SaveAuditLogEntryAsync(entry, cancellationToken);

    public async Task<ListResponse<AuditLogEntryDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var entries = await moderationRepository.ListAuditLogEntriesAsync(cancellationToken);
        var filtered = entries
            .Where(entry => !query.ActorUserId.HasValue || entry.ActorUserId == query.ActorUserId.Value)
            .Where(entry => string.IsNullOrWhiteSpace(query.TargetEntityType) || string.Equals(entry.TargetEntityType, query.TargetEntityType.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(entry => !query.TargetEntityId.HasValue || entry.TargetEntityId == query.TargetEntityId.Value)
            .Select(entry => new AuditLogEntryDto(
                entry.Id,
                entry.ActionType,
                entry.ActorUserId,
                entry.TargetEntityType,
                entry.TargetEntityId,
                entry.CreatedAtUtc,
                entry.Details))
            .ToArray();
        var items = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new ListResponse<AuditLogEntryDto>(items, filtered.Length);
    }
}
