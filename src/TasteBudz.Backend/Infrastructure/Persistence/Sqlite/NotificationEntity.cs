namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class NotificationEntity
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }
    public Domain.NotificationType NotificationType { get; set; }
    public string ContextType { get; set; } = string.Empty;
    public Guid? ContextId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
}
