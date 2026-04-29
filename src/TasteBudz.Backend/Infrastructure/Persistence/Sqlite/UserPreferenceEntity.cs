namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class UserPreferenceEntity
{
    public Guid UserId { get; set; }
    public Domain.SpiceTolerance? SpiceTolerance { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
