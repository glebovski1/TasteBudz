namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class EventParticipantEntity
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public Domain.EventParticipantState State { get; set; }
    public DateTimeOffset? InvitedAtUtc { get; set; }
    public DateTimeOffset? JoinedAtUtc { get; set; }
    public DateTimeOffset? RespondedAtUtc { get; set; }
    public DateTimeOffset? LeftAtUtc { get; set; }
    public DateTimeOffset? RemovedAtUtc { get; set; }
}
