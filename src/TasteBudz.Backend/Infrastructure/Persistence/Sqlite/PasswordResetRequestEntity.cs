namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class PasswordResetRequestEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? MatchedUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid? ClosedByUserId { get; set; }
}
