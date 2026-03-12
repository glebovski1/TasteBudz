namespace TasteBudz.Backend.Modules.Moderation;

public sealed record AuditLogEntryDto(
    Guid AuditLogEntryId,
    string ActionType,
    Guid ActorUserId,
    string TargetEntityType,
    Guid? TargetEntityId,
    DateTimeOffset CreatedAtUtc,
    string Details);
