namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class ModerationActionEntity
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid? ReportId { get; set; }
    public Domain.ModerationActionType ActionType { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
