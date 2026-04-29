namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class CheckoutSessionEntity
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public Domain.CheckoutSessionStatus Status { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int SubtotalCents { get; set; }
    public int DiscountCents { get; set; }
    public int TotalCents { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
}
