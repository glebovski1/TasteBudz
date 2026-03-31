using Microsoft.Data.Sqlite;

namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Normalizes configured SQLite connection strings so relative file paths resolve predictably.
/// </summary>
public static class SqliteConnectionStringHelper
{
    public static string Normalize(string connectionString, string basePath)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:TasteBudz must be configured.");
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (!string.IsNullOrWhiteSpace(builder.DataSource) &&
            !Path.IsPathRooted(builder.DataSource) &&
            !string.Equals(builder.DataSource, ":memory:", StringComparison.Ordinal))
        {
            builder.DataSource = Path.GetFullPath(Path.Combine(basePath, builder.DataSource));
        }

        return builder.ToString();
    }
}
