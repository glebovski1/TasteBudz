namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class GroupEntity
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Domain.GroupVisibility Visibility { get; set; }
    public Domain.GroupWallpaperTheme WallpaperTheme { get; set; }
    public Domain.GroupLifecycleState LifecycleState { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
