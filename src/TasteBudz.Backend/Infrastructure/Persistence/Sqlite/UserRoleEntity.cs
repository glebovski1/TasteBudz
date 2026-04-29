namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class UserRoleEntity
{
    public Guid UserId { get; set; }
    public Domain.UserRole Role { get; set; }
}
