namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Controls provider selection and local/test SQLite bootstrap behavior.
/// </summary>
public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public string Provider { get; set; } = TasteBudz.Backend.Infrastructure.Persistence.PersistenceProviderNames.Sqlite;

    public bool InitializeSqliteOnStartup { get; set; }

    public bool SeedTestDataOnStartup { get; set; }
}
