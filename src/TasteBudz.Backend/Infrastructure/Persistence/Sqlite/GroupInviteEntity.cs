namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class GroupInviteEntity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid InvitedUserId { get; set; }
    public Guid InviterUserId { get; set; }
    public Domain.GroupInviteStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
