namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class ChatThreadEntity
{
    public Guid Id { get; set; }
    public Domain.ChatScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
