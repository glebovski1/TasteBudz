namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class RestaurantSlotEntity
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public int Capacity { get; set; }
    public DateTimeOffset CutoffAtUtc { get; set; }
    public int? MinThresholdForDiscount { get; set; }
    public int? DiscountPercent { get; set; }
    public Domain.RestaurantSlotStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
}
