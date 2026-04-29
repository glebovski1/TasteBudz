namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class MediaAssetEntity
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? ProfileUserId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? ReportId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public byte[] Content { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
}
