namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class RecurringAvailabilityWindowEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Label { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
