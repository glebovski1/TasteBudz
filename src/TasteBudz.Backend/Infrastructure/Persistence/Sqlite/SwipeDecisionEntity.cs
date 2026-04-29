namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class SwipeDecisionEntity
{
    public Guid ActorUserId { get; set; }
    public Guid SubjectUserId { get; set; }
    public Domain.SwipeDecisionType Decision { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
