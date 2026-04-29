namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class BudConnectionEntity
{
    public Guid Id { get; set; }
    public Guid UserOneId { get; set; }
    public Guid UserTwoId { get; set; }
    public Domain.BudConnectionState State { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
}
