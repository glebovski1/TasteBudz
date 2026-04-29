namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class EventEntity
{
    public Guid Id { get; set; }
    public Guid HostUserId { get; set; }
    public string? Title { get; set; }
    public Domain.EventType EventType { get; set; }
    public Domain.EventStatus Status { get; set; }
    public DateTimeOffset EventStartAtUtc { get; set; }
    public DateTimeOffset DecisionAtUtc { get; set; }
    public int Capacity { get; set; }
    public int MinParticipantsToRun { get; set; }
    public Guid? SelectedRestaurantId { get; set; }
    public string? CuisineTarget { get; set; }
    public Guid? GroupId { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
