namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class PrivacySettingEntity
{
    public Guid UserId { get; set; }
    public bool DiscoveryEnabled { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
