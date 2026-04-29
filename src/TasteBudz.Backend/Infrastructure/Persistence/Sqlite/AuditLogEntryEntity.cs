namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class AuditLogEntryEntity
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid? TargetEntityId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string Details { get; set; } = string.Empty;
}
