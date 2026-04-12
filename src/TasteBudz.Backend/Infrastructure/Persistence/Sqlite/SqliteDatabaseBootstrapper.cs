using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TasteBudz.Backend.Infrastructure.Persistence;

namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Creates and validates the SQLite schema from the repository's canonical SQL assets.
/// </summary>
public static class SqliteDatabaseBootstrapper
{
    public static async Task EnsureInitializedAsync(
        string connectionString,
        bool initializeOnStartup,
        bool seedTestDataOnStartup,
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
            var shouldSeedTestData =
                seedTestDataOnStartup &&
                !await HasAnyUserAccountsAsync(connectionString, cancellationToken);

            if (seedTestDataOnStartup && !shouldSeedTestData)
            {
                logger.LogInformation("Skipping development test-data seed because the SQLite database already contains user accounts.");
            }

            await InitializeSchemaAsync(connectionString, shouldSeedTestData, cancellationToken);
        }

        await ValidateRequiredSchemaAsync(connectionString, cancellationToken);
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

        await InitializeSchemaAsync(connectionString, seedTestData: false, cancellationToken);
        await ValidateRequiredSchemaAsync(connectionString, cancellationToken);
    }

    private static async Task InitializeSchemaAsync(string connectionString, bool seedTestData, CancellationToken cancellationToken)
    {
        var schemaScript = await File.ReadAllTextAsync(GetBundledScriptPath("dbTasteBudz.sqlite.sql"), cancellationToken);
        var seedScript = await File.ReadAllTextAsync(GetBundledScriptPath("dbTasteBudz.sqlite.seed.sql"), cancellationToken);
        var testDataScript = seedTestData
            ? await File.ReadAllTextAsync(GetBundledScriptPath("dbTasteBudz.sqlite.testdata.sql"), cancellationToken)
            : null;
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
        command.CommandText = string.Join(
            Environment.NewLine,
            new[] { schemaScript, seedScript, testDataScript }.Where(script => !string.IsNullOrWhiteSpace(script)));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ValidateRequiredSchemaAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var (tableName, requiredColumns) in EfCoreSchemaRequirements.GetRequiredTableColumns())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            command.Parameters.AddWithValue("$name", tableName);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

            if (count != 1)
            {
                throw new InvalidOperationException($"The configured SQLite database is missing required table '{tableName}'.");
            }

            var actualColumns = await ListColumnsAsync(connection, tableName, cancellationToken);

            foreach (var requiredColumn in requiredColumns)
            {
                if (!actualColumns.Contains(requiredColumn))
                {
                    throw new InvalidOperationException($"The configured SQLite database table '{tableName}' is missing required column '{requiredColumn}'.");
                }
            }
        }
    }

    private static async Task<HashSet<string>> ListColumnsAsync(
        SqliteConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteSqliteIdentifier(tableName)});";

        var columns = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(reader.GetOrdinal("name")));
        }

        return columns;
    }

    private static async Task<bool> HasAnyUserAccountsAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (!string.IsNullOrWhiteSpace(builder.DataSource) &&
            !string.Equals(builder.DataSource, ":memory:", StringComparison.Ordinal))
        {
            var directory = Path.GetDirectoryName(builder.DataSource);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                return false;
            }

            if (!File.Exists(builder.DataSource))
            {
                return false;
            }
        }

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'UserAccounts';";

        var hasUserAccountsTable = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;

        if (!hasUserAccountsTable)
        {
            return false;
        }

        command.CommandText = "SELECT COUNT(*) FROM UserAccounts;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static string QuoteSqliteIdentifier(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string GetBundledScriptPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "DatabaseScripts", "sqlite", fileName);
}
