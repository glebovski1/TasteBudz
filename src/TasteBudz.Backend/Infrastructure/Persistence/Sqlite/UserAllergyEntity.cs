namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class UserAllergyEntity
{
    public Guid UserId { get; set; }
    public string Allergy { get; set; } = string.Empty;
}
