namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class EventFeedbackEntity
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid AuthorUserId { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
