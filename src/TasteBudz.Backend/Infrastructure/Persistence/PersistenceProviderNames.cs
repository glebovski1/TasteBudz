namespace TasteBudz.Backend.Infrastructure.Persistence;

public static class PersistenceProviderNames
{
    public const string Sqlite = "Sqlite";
    public const string SqlServer = "SqlServer";

    public static string Normalize(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return Sqlite;
        }

        if (string.Equals(provider, Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            return Sqlite;
        }

        if (string.Equals(provider, SqlServer, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "AzureSql", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "AzureSqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return SqlServer;
        }

        throw new InvalidOperationException(
            $"Unsupported persistence provider '{provider}'. Use '{Sqlite}' or '{SqlServer}'.");
    }
}
