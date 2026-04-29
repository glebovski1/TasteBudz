namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class UserDietaryFlagEntity
{
    public Guid UserId { get; set; }
    public string DietaryFlag { get; set; } = string.Empty;
}
