namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Controls local/test SQLite bootstrap behavior.
/// </summary>
public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public bool InitializeSqliteOnStartup { get; set; }

    public bool SeedTestDataOnStartup { get; set; }
}
