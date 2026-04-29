namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class GroupMemberEntity
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public Domain.GroupMemberState State { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
