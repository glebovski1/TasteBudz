namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class EventSlotReservationEntity
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid SlotId { get; set; }
    public Domain.EventSlotReservationStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
}
