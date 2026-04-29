namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class UserProfileEntity
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string HomeAreaZipCode { get; set; } = string.Empty;
    public Domain.SocialGoal? SocialGoal { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
