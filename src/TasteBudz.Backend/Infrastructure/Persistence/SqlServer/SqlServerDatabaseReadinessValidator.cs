using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using TasteBudz.Backend.Infrastructure.Persistence;

namespace TasteBudz.Backend.Infrastructure.Persistence.SqlServer;

public static class SqlServerDatabaseReadinessValidator
{
    public static async Task ValidateRequiredSchemaAsync(
        string connectionString,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:TasteBudz must be configured for SQL Server persistence.");
        }

        ValidateConnectionStringLooksLikeSqlServer(connectionString);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var (tableName, requiredColumns) in EfCoreSchemaRequirements.GetRequiredTableColumns())
        {
            await using var tableCommand = connection.CreateCommand();
            tableCommand.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @name;";
            tableCommand.Parameters.AddWithValue("@name", tableName);

            var tableCount = Convert.ToInt32(await tableCommand.ExecuteScalarAsync(cancellationToken));

            if (tableCount != 1)
            {
                throw new InvalidOperationException($"The configured SQL Server database is missing required table '{tableName}'.");
            }

            var actualColumns = await ListColumnsAsync(connection, tableName, cancellationToken);

            foreach (var requiredColumn in requiredColumns)
            {
                if (!actualColumns.Contains(requiredColumn))
                {
                    throw new InvalidOperationException($"The configured SQL Server database table '{tableName}' is missing required column '{requiredColumn}'.");
                }
            }
        }

        logger.LogInformation("SQL Server backend persistence is ready.");
    }

    private static void ValidateConnectionStringLooksLikeSqlServer(string connectionString)
    {
        if (connectionString.Contains("Foreign Keys", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains(".sqlite", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Persistence:Provider is set to SqlServer, but ConnectionStrings:TasteBudz appears to be a SQLite connection string. Configure ConnectionStrings:TasteBudz with an Azure SQL or SQL Server connection string.");
        }

        try
        {
            _ = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:TasteBudz must be a valid Azure SQL or SQL Server connection string when Persistence:Provider is SqlServer.",
                exception);
        }
    }

    private static async Task<HashSet<string>> ListColumnsAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @name;";
        command.Parameters.AddWithValue("@name", tableName);

        var columns = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }
}
