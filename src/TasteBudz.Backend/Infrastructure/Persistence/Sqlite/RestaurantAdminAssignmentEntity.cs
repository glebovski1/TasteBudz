namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class RestaurantAdminAssignmentEntity
{
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}
