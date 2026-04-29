namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class UserRestrictionEntity
{
    public Guid Id { get; set; }
    public Guid SubjectUserId { get; set; }
    public Guid IssuedByUserId { get; set; }
    public Guid? ModerationActionId { get; set; }
    public Domain.RestrictionScope Scope { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public Domain.RestrictionStatus Status { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}
