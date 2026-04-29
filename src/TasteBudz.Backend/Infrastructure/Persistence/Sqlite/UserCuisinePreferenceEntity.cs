namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class UserCuisinePreferenceEntity
{
    public Guid UserId { get; set; }
    public Guid CuisineId { get; set; }
}
