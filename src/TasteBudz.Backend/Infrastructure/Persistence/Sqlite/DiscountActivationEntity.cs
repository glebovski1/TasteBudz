namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class DiscountActivationEntity
{
    public Guid ReservationId { get; set; }
    public bool IsActive { get; set; }
    public bool IsFinalized { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
}
