namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class GroupAnnouncementEntity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid AuthorUserId { get; set; }
    public Domain.GroupAnnouncementType AnnouncementType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? RelatedEventId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
