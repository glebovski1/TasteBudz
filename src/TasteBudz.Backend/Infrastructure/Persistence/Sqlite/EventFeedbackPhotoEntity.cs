namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class EventFeedbackPhotoEntity
{
    public Guid EventFeedbackId { get; set; }
    public Guid MediaAssetId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
