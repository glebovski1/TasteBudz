using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Creates and validates the SQLite schema from the repository's canonical SQL assets.
/// </summary>
public static class SqliteDatabaseBootstrapper
{
    private static readonly Lazy<string[]> RequiredTables = new(BuildRequiredTables);

    public static async Task EnsureInitializedAsync(
        string connectionString,
        bool initializeOnStartup,
        string environmentName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var canInitialize =
            initializeOnStartup &&
            (string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(environmentName, "IntegrationTesting", StringComparison.OrdinalIgnoreCase));

        if (canInitialize)
        {
            await InitializeSchemaAsync(connectionString, cancellationToken);
        }

        await ValidateRequiredTablesAsync(connectionString, cancellationToken);
        logger.LogInformation("SQLite backend persistence is ready.");
    }

    public static async Task RecreateDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (!string.IsNullOrWhiteSpace(dataSource) &&
            !string.Equals(dataSource, ":memory:", StringComparison.Ordinal))
        {
            var directory = Path.GetDirectoryName(dataSource);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(dataSource))
            {
                File.Delete(dataSource);
            }
        }

        await InitializeSchemaAsync(connectionString, cancellationToken);
        await ValidateRequiredTablesAsync(connectionString, cancellationToken);
    }

    private static async Task InitializeSchemaAsync(string connectionString, CancellationToken cancellationToken)
    {
        var schemaScript = await File.ReadAllTextAsync(GetBundledScriptPath("dbTasteBudz.sqlite.sql"), cancellationToken);
        var seedScript = await File.ReadAllTextAsync(GetBundledScriptPath("dbTasteBudz.sqlite.seed.sql"), cancellationToken);
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (!string.IsNullOrWhiteSpace(builder.DataSource) &&
            !string.Equals(builder.DataSource, ":memory:", StringComparison.Ordinal))
        {
            var directory = Path.GetDirectoryName(builder.DataSource);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"{schemaScript}{Environment.NewLine}{seedScript}";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ValidateRequiredTablesAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var tableName in RequiredTables.Value)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            command.Parameters.AddWithValue("$name", tableName);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

            if (count != 1)
            {
                throw new InvalidOperationException($"The configured SQLite database is missing required table '{tableName}'.");
            }
        }
    }

    private static string[] BuildRequiredTables()
    {
        var options = new DbContextOptionsBuilder<TasteBudzDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var dbContext = new TasteBudzDbContext(options);

        return dbContext.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Where(tableName => !string.IsNullOrWhiteSpace(tableName))
            .Select(tableName => tableName!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(tableName => tableName, StringComparer.Ordinal)
            .ToArray();
    }

    private static string GetBundledScriptPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "DatabaseScripts", fileName);
}
