namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class ChatMessageEntity
{
    public Guid Id { get; set; }
    public Guid ThreadId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
