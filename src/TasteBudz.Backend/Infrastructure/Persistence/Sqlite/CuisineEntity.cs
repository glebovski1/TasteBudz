namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class CuisineEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
