namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class UserBlockEntity
{
    public Guid BlockerUserId { get; set; }
    public Guid BlockedUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
